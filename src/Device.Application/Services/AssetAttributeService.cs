using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using AHI.Infrastructure.UserContext.Abstraction;
using Device.Application.Asset.Command;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Events;
using Device.Application.Exception;
using Device.Application.Model;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.Application.SharedKernel;
using Device.ApplicationExtension.Extension;

namespace Device.Application.Service
{
    public class AssetAttributeService : IAssetAttributeService
    {
        private readonly IAuditLogService _auditLogService;
        private readonly IAssetUnitOfWork _unitOfWork;
        private readonly IAssetAttributeHandler _assetAttributeHandler;
        private readonly IAssetService _assetService;
        private readonly ITenantContext _tenantContext;
        private readonly IEntityLockService _entityLockService;
        private readonly IUserContext _userContext;
        private readonly ILoggerAdapter<AssetAttributeService> _logger;
        private readonly IReadAssetAttributeRepository _readAssetAttributeRepository;
        private readonly IReadAssetRepository _readAssetRepository;
        private readonly IReadAssetAttributeTemplateRepository _readAssetAttributeTemplateRepository;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ICache _cache;

        public AssetAttributeService(
            IAuditLogService activityLogService,
            IAssetUnitOfWork unitOfWork,
            IAssetAttributeHandler assetAttributeHandler,
            IAssetService assetService,
            ITenantContext tenantContext,
            IEntityLockService entityLockService,
            IUserContext userContext,
            ILoggerAdapter<AssetAttributeService> logger,
            IConfiguration configuration,
            IReadAssetAttributeRepository readAssetAttributeRepository,
            IReadAssetRepository readAssetRepository,
            IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository,
            IServiceScopeFactory scopeFactor,
            ICache cache)
        {
            _auditLogService = activityLogService;
            _unitOfWork = unitOfWork;
            _assetAttributeHandler = assetAttributeHandler;
            _assetService = assetService;
            _tenantContext = tenantContext;
            _entityLockService = entityLockService;
            _userContext = userContext;
            _logger = logger;
            _readAssetAttributeRepository = readAssetAttributeRepository;
            _readAssetRepository = readAssetRepository;
            _readAssetAttributeTemplateRepository = readAssetAttributeTemplateRepository;
            _scopeFactory = scopeFactor;
            _cache = cache;
        }

        public async Task<UpsertAssetAttributeDto> UpsertAssetAttributeAsync(UpsertAssetAttribute command, CancellationToken token)
        {
            JsonPatchDocument document = command.Data;
            var assetId = command.AssetId;
            UpsertAssetAttributeDto result = null;

            // Including the Attribute's name come from Request of Add & Edit action - For Delete, we will only load from DB.
            var addEditAttributes = document.Operations
                                            .Where(x => x.op != PatchActionConstants.REMOVE)
                                            .Select(x => x.value.ToJson().FromJson<Asset.Command.AssetAttribute>())
                                            .Select(a => new KeyValuePair<Guid, string>(a.Id, a.Name));
            var deleteAttributes = document.Operations
                                            .Where(x => x.op == PatchActionConstants.REMOVE)
                                            .Select(x => x.value.ToJson().FromJson<DeleteAssetAttribute>())
                                            .SelectMany(a => a.Ids.Select(id => new KeyValuePair<Guid, string>(id, null)));
            var auditAttributes = addEditAttributes.Union(deleteAttributes).ToList();

            ActionType mainAction = document.Operations.All(x => string.Equals(x.op, PatchActionConstants.REMOVE, StringComparison.InvariantCultureIgnoreCase))
                                    ? ActionType.Delete
                                    : ActionType.Update;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var isLocked = await _entityLockService.ValidateEntityLockedByOtherAsync(new EntityLock.Command.ValidateLockEntityCommand()
                {
                    HolderUpn = _userContext.Upn,
                    TargetId = command.AssetId
                }, token);
                if (isLocked)
                {
                    throw LockException.CreateAlreadyLockException();
                }
                var data = await ProcessUpsertAssetAttributeAsync(document, assetId, auditAttributes, token);
                result = new UpsertAssetAttributeDto
                {
                    Data = data
                };
                await _unitOfWork.CommitAsync();
            }
            catch (System.Exception exc)
            {
                _logger.LogError(exc, exc.Message);
                await _unitOfWork.RollbackAsync();
                var message = exc.Message;
                if (exc is EntityValidationException validationException)
                {
                    message = validationException.DetailCode;
                }

                if (message != MessageConstants.ASSET_ATTRIBUTE_HAVE_REFERENCE)
                {
                    await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_ATTRIBUTES,
                                                        mainAction,
                                                        ActionStatus.Fail,
                                                        auditAttributes.Select(x => x.Key), // Attribute Id
                                                        auditAttributes.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value), // Attribute Name
                                                        new { Message = message },
                                                        command);
                }
                throw;
            }

            // need to clear cache immediately to show correct data on UI
            var hashKey = CacheKey.ASSET_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);

            var attributesHashKey = CacheKey.ALL_ALIAS_REFERENCE_ATTRIBUTES_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);
            var attributeIdsHashKey = CacheKey.ALIAS_REFERENCE_ID_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);
            await _cache.ClearHashAsync(hashKey);
            await _cache.ClearHashAsync(attributesHashKey);
            await _cache.ClearHashAsync(attributeIdsHashKey);

            DoPostUpsertAssetAttributeFireAndForget(command, assetId, auditAttributes, mainAction, _tenantContext, _userContext);
            return result;
        }

        private void DoPostUpsertAssetAttributeFireAndForget(UpsertAssetAttribute command, Guid assetId, List<KeyValuePair<Guid, string>> auditAttributes, ActionType mainAction, ITenantContext tenantContextSource, IUserContext userContextSource)
        {
            Task.Run(async () => await DoPostUpsertAssetAttributeAsync(command, assetId, auditAttributes, mainAction, tenantContextSource, userContextSource));
        }

        private async Task DoPostUpsertAssetAttributeAsync(UpsertAssetAttribute command, Guid assetId, List<KeyValuePair<Guid, string>> auditAttributes, ActionType mainAction, ITenantContext tenantContextSource, IUserContext userContextSource)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                tenantContext.CopyFrom(tenantContextSource);
                var userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
                userContext.CopyFrom(userContextSource);

                IAuditLogService auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
                IReadAssetAttributeRepository readAssetAttributeRepository = scope.ServiceProvider.GetRequiredService<IReadAssetAttributeRepository>();
                DeviceBackgroundService deviceBackgroundService = scope.ServiceProvider.GetRequiredService<DeviceBackgroundService>();
                IDomainEventDispatcher domainEventDispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

                var attributeIds = auditAttributes.Select(x => x.Key);
                var runtimeAttributes = await readAssetAttributeRepository.AsQueryable()
                                                .Include(x => x.AssetAttributeRuntime).ThenInclude(x => x.Triggers)
                                                .AsNoTracking()
                                                .Where(x => x.AssetId == command.AssetId
                                                        && x.AttributeType == AttributeTypeConstants.TYPE_RUNTIME
                                                )
                                                .ToListAsync();
                var relatedAttributeIds = runtimeAttributes.SelectMany(x => x.AssetAttributeRuntime.Triggers.Select(s => (s.TriggerAttributeId, x.Id)));
                var relatedAttributeRuntimeIds = relatedAttributeIds.Where(x => attributeIds.Any(a => a == x.TriggerAttributeId)).Select(x => x.Id).Distinct();
                var effectedRuntimeAttributes = runtimeAttributes.Where(x => attributeIds.Any(a => a == x.Id) || relatedAttributeRuntimeIds.Contains(x.Id))
                                                                    .Select(x => new AssetAttributeDto()
                                                                    {
                                                                        AssetId = assetId,
                                                                        Id = x.Id,
                                                                        AttributeType = x.AttributeType
                                                                    })
                                                                    .ToArray();

                await deviceBackgroundService.QueueAsync(tenantContext, new CleanAssetCache(effectedRuntimeAttributes));
                await domainEventDispatcher.SendAsync(new AssetAttributeChangedEvent(assetId, 0, tenantContext, forceReload: true));

                await auditLogService.SendLogAsync(ActivityEntityAction.ASSET_ATTRIBUTES,
                                                    mainAction,
                                                    ActionStatus.Success,
                                                    auditAttributes.Select(x => x.Key), // Attribute Id
                                                    auditAttributes.Select(x => x.Value), // Attribute Name
                                                    command);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private async Task<IList<BaseJsonPathDocument>> ProcessUpsertAssetAttributeAsync(JsonPatchDocument document,
                                                                                        Guid assetId,
                                                                                        ICollection<KeyValuePair<Guid, string>> auditAttributes,
                                                                                        CancellationToken cancellationToken)
        {
            string path;
            Guid attributeId;
            var operations = document.Operations;
            var resultModels = new List<BaseJsonPathDocument>();
            int index = 0;

            var inputAttributes = operations
                                        .Where(x => x.op == PatchActionConstants.ADD || x.op == PatchActionConstants.EDIT)
                                        .Select(x => x.value.ToJson().FromJson<Asset.Command.AssetAttribute>());
            foreach (Operation operation in operations)
            {
                index++;
                var resultModel = new BaseJsonPathDocument
                {
                    OP = operation.op,
                    Path = operation.path
                };
                switch (operation.op)
                {
                    case PatchActionConstants.ADD:
                        var attribute = operation.value.ToJson().FromJson<Asset.Command.AssetAttribute>();
                        attribute.AssetId = assetId;
                        attribute.SequentialNumber = index;
                        attribute.DecimalPlace = GetAttributeDecimalPlace(attribute);
                        var result = await _assetAttributeHandler.AddAsync(attribute, inputAttributes, cancellationToken);
                        break;

                    case PatchActionConstants.EDIT:
                        path = operation.path.Replace("/", "");
                        if (Guid.TryParse(path, out attributeId))
                        {
                            var updateAttribute = operation.value.ToJson().FromJson<Asset.Command.AssetAttribute>();
                            updateAttribute.AssetId = assetId;
                            updateAttribute.Id = attributeId;
                            updateAttribute.DecimalPlace = GetAttributeDecimalPlace(updateAttribute);
                            var updateResult = await _assetAttributeHandler.UpdateAsync(updateAttribute, inputAttributes, cancellationToken);
                        }
                        break;

                    case PatchActionConstants.EDIT_TEMPLATE:
                        path = operation.path.Replace("/", "");
                        if (Guid.TryParse(path, out attributeId))
                        {
                            var updateElement = operation.value.ToJson().FromJson<Asset.Command.AssetAttribute>();

                            await ValidateForEditTemplateAssetAttributeAsync(assetId, updateElement);
                        }
                        break;

                    case PatchActionConstants.REMOVE:
                        var command = operation.value.ToJson().FromJson<DeleteAssetAttribute>();
                        if (command.Ids.Any())
                        {
                            await RemoveAssetAttributeAsync(command, auditAttributes, assetId);
                            resultModel.Values = operation;
                        }
                        break;
                }
                resultModels.Add(resultModel);
            }

            return resultModels;
        }

        private int? GetAttributeDecimalPlace(Asset.Command.AssetAttribute attribute)
        {
            return attribute.DataType == DataTypeConstants.TYPE_DOUBLE ? attribute.DecimalPlace : null;
        }

        private async Task RemoveAssetAttributeAsync(DeleteAssetAttribute deleteAssetAttribute,
                                                    ICollection<KeyValuePair<Guid, string>> auditAttributes,
                                                    Guid assetId)
        {
            if (!deleteAssetAttribute.ForceDelete)
            {
                var dependencies = await _assetService.GetDependencyOfAttributeAsync(deleteAssetAttribute.Ids);

                if (dependencies.Any())
                {
                    // bad news, cannot delete the asset attribute.
                    throw EntityValidationExceptionHelper.GenerateException(
                            ErrorPropertyConstants.AssetAttribute.ATTRIBUTE_ID,
                            fieldErrorMessage: MessageConstants.ASSET_ATTRIBUTE_USING,
                            detailCode: MessageConstants.ASSET_ATTRIBUTE_HAVE_REFERENCE,
                            payload: new Dictionary<string, object>
                            {
                                {"entityInUsed", dependencies}
                            });
                }
            }

            var lstDeletingAttributes = await _unitOfWork.AssetAttributes
                                                    .AsQueryable()
                                                    .Where(aa => deleteAssetAttribute.Ids.Contains(aa.Id))
                                                    .ToListAsync();

            if (lstDeletingAttributes.Count != deleteAssetAttribute.Ids.Count())
            {
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
            }

            foreach (var attribute in lstDeletingAttributes)
            {
                // As request of deleting attributes do not contains the Attribute Name => Loading from DB.
                var auditItem = auditAttributes.First(a => a.Key == attribute.Id); // Cannot null
                auditAttributes.Remove(auditItem);
                auditAttributes.Add(new KeyValuePair<Guid, string>(attribute.Id, attribute.Name));

                await _unitOfWork.AssetAttributes.RemoveAsync(attribute.Id);
                if (attribute.AttributeType == AttributeTypeConstants.TYPE_RUNTIME)
                    await _unitOfWork.AssetAttributes.RemoveAssetRuntimeAttributeTriggersAsync(attribute.Id);
            }

            var triggerCacheKey = CacheKey.FUNCTION_BLOCK_TRIGGER_KEY.GetCacheKey(assetId);
            await _cache.DeleteAsync(triggerCacheKey);
        }

        public async Task<IEnumerable<ValidateAssetAttributesDto>> ValidateAssetAttributesSeriesAsync(ValidateAssetAttributeSeries request, CancellationToken token)
        {
            var assetAttributesSeries = await _readAssetAttributeRepository.QueryAssetAttributeSeriesDataAsync(request.AttributeIds);
            var assetAttributes = assetAttributesSeries.GroupBy(t => t.AssetId).Select(s =>
            {
                var assetId = s.Key;
                var attributeIds = s.GroupBy(att => att.AttributeId).Select(att => { return att.Key; });
                return new ValidateAssetAttributesDto(assetId, attributeIds);
            });
            return assetAttributes;
        }

        public async Task<BaseResponse> ValidateDeleteAssetTemplateAttributeAsync(Guid attributeId, CancellationToken token)
        {
            // i/o bound issue
            var existAttribute = await _readAssetAttributeRepository.FindAsync(attributeId);
            if (existAttribute == null)
                throw new EntityNotFoundException();

            await _readAssetAttributeTemplateRepository.ValidateRemoveAttributeAsync(existAttribute.Id);
            return BaseResponse.Success;
        }

        private async Task ValidateForEditTemplateAssetAttributeAsync(Guid assetId, Asset.Command.AssetAttribute updateElement)
        {
            // check asset_attribute_template, and just update type static
            var asset = await _readAssetRepository
                                    .AsQueryable()
                                    .Include(x => x.AssetTemplate).ThenInclude(x => x.Attributes)
                                    .AsNoTracking()
                                    .Where(x => x.Id == assetId)
                                    .FirstOrDefaultAsync();
            if (asset != null)
            {
                var isCheckValid = asset.AssetTemplate.Attributes.FirstOrDefault(x => x.AttributeType == AttributeTypeConstants.TYPE_STATIC);
                if (isCheckValid != null)
                {
                    await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_ATTRIBUTES, ActionType.Update, ActionStatus.Fail, updateElement.AssetId, payload: updateElement);
                    throw new EntityInvalidException(detailCode: MessageConstants.ASSET_ATTRIBUTE_TYPE_INVALID);
                }
            }
        }
    }
}
