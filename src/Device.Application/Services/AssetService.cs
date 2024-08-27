using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AHI.Device.Application.Constant;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Enum;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.Security.Extension;
using AHI.Infrastructure.Service;
using AHI.Infrastructure.Service.Tag.Extension;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using AHI.Infrastructure.SharedKernel.Models;
using AHI.Infrastructure.UserContext.Abstraction;
using AHI.Infrastructure.UserContext.Service.Abstraction;
using AHI.Infrastructure.Validation.Abstraction;
using Device.Application.AlarmRule.Query;
using Device.Application.Asset;
using Device.Application.Asset.Command;
using Device.Application.Asset.Command.Model;
using Device.Application.BlockFunction.Query;
using Device.Application.Constant;
using Device.Application.Device.Command;
using Device.Application.Device.Command.Model;
using Device.Application.EventForwarding.Command;
using Device.Application.Events;
using Device.Application.Exception;
using Device.Application.Helper;
using Device.Application.Models;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.ApplicationExtension.Extension;
using Device.Domain.Entity;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Device.Application.Service
{
    public class AssetService : BaseSearchService<Domain.Entity.Asset, Guid, GetAssetByCriteria, GetAssetSimpleDto>, IAssetService
    {
        private const string TriggerAttributeId = "TriggerAttributeId";
        private const string Expression = "Expression";
        private static string[] supportedAttributeTypes = new[]
        {
            AttributeTypeConstants.TYPE_DYNAMIC,
            AttributeTypeConstants.TYPE_INTEGRATION
        };

        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;
        private readonly IAssetUnitOfWork _unitOfWork;
        private readonly IUserContext _userContext;
        private readonly ITenantContext _tenantContext;
        private readonly ISecurityService _securityService;
        private readonly IDynamicValidator _dynamicValidator;
        private readonly IDomainEventDispatcher _dispatcher;
        private readonly ILoggerAdapter<AssetService> _logger;
        private readonly IAttributeMappingHandler _attributeMappingHandler;
        private readonly IEntityLockService _entityLockService;
        private readonly IAssetAttributeHandler _assetAttributeHandler;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DeviceBackgroundService _deviceBackgroundService;
        private readonly IDeviceService _deviceService;
        private readonly ISecurityContext _securityContext;
        private readonly ICache _cache;
        private readonly IValidator<ArchiveAssetDto> _assetVerifyValidator;
        private readonly IMediator _mediator;
        private readonly IFileEventService _fileEventService;
        private readonly IReadAssetTemplateRepository _readAssetTemplateRepository;
        private readonly IReadDeviceRepository _readDeviceRepository;
        private readonly IReadAssetAttributeRepository _readAssetAttributeRepository;
        private readonly IReadAssetRepository _readAssetRepository;
        private readonly IReadDeviceTemplateRepository _readDeviceTemplateRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly ITagService _tagService;

        public AssetService(
            IServiceProvider serviceProvider,
            IAuditLogService activityLogService,
            INotificationService notificationService,
            IAssetUnitOfWork unitOfWork,
            IUserContext userContext,
            ITenantContext tenantContext,
            IDomainEventDispatcher dispatcher,
            ISecurityService securityService,
            IDynamicValidator dynamicValidator,
            IAttributeMappingHandler attributeMappingHandler,
            ILoggerAdapter<AssetService> logger,
            IAssetAttributeHandler assetAttributeHandler,
            IHttpClientFactory httpClientFactory,
            IEntityLockService entityLockService,
            DeviceBackgroundService deviceBackgroundService,
            IDeviceService deviceService,
            ISecurityContext securityContext,
            ICache cache,
            IValidator<ArchiveAssetDto> assetVerifyValidator,
            IConfiguration configuration,
            IMediator mediator,
            IFileEventService fileEventService,
            IReadAssetTemplateRepository readAssetTemplateRepository,
            IReadDeviceRepository readDeviceRepository,
            IReadAssetAttributeRepository readAssetAttributeRepository,
            IReadAssetRepository readAssetRepository,
            IReadDeviceTemplateRepository readDeviceTemplateRepository,
            ITagService tagService,
            IAssetRepository assetRepository)
             : base(GetAssetSimpleDto.Create, serviceProvider)
        {
            _auditLogService = activityLogService;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _userContext = userContext;
            _tenantContext = tenantContext;
            _securityService = securityService;
            _dynamicValidator = dynamicValidator;
            _dispatcher = dispatcher;
            _attributeMappingHandler = attributeMappingHandler;
            _logger = logger;
            _entityLockService = entityLockService;
            _assetAttributeHandler = assetAttributeHandler;
            _httpClientFactory = httpClientFactory;
            _deviceBackgroundService = deviceBackgroundService;
            _deviceService = deviceService;
            _securityContext = securityContext;
            _cache = cache;
            _assetVerifyValidator = assetVerifyValidator;
            _mediator = mediator;
            _fileEventService = fileEventService;
            _readAssetTemplateRepository = readAssetTemplateRepository;
            _readDeviceRepository = readDeviceRepository;
            _readAssetAttributeRepository = readAssetAttributeRepository;
            _readAssetRepository = readAssetRepository;
            _readDeviceTemplateRepository = readDeviceTemplateRepository;
            _assetRepository = assetRepository;
            _tagService = tagService;
        }

        protected override Type GetDbType()
        {
            return typeof(IAssetRepository);
        }

        public async Task<IEnumerable<AssetPathDto>> GetPathsAsync(GetAssetPath request, CancellationToken cancellationToken)
        {
            var paths = new List<AssetPathDto>();
            IEnumerable<AssetPath> assetPaths = await _assetRepository.GetPathsAsync(request.AssetIds);

            foreach (AssetPath assetPath in assetPaths)
            {
                var assetPathDto = new AssetPathDto(assetPath.Id, assetPath.AssetPathId, assetPath.AssetPathName);
                if (request.IncludeAttribute)
                {
                    var asset = await FindAssetByIdAsync(new GetAssetById(assetPath.Id), cancellationToken);
                    if (asset != null)
                        assetPathDto.Attributes = asset.Attributes;
                }

                paths.Add(assetPathDto);
            }
            return paths;
        }

        public Task<IEnumerable<Guid>> CheckExistingAssetIdsAsync(CheckExistingAssetIds request, CancellationToken cancellationToken)
        {
            return _readAssetRepository.GetExistingAssetIdsAsync(request.AssetIds);
        }

        public async Task<GetAssetDto> FindAssetByIdOptimizedAsync(GetAssetById command, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            var (asset, hashField) = await TryGetAssetFromCache(command.Id, command.UseCache);

            if (asset == null)
            {
                asset = await GetAssetByIdFromDbOptimizedAsync(command);

                var hashKey = CacheKey.ASSET_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);
                await _cache.SetHashByKeyAsync(hashKey, hashField, asset);
            }

            _logger.LogTrace($"Get asset by id {command.Id} take {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
            return await _tagService.FetchTagsAsync(asset);
        }

        public async Task<GetAssetDto> FindAssetByIdAsync(GetAssetById command, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            var (asset, hashField) = await TryGetAssetFromCache(command.Id, command.UseCache);

            if (asset == null)
            {
                asset = await GetAssetByIdFromDbAsync(command);

                var hashKey = CacheKey.ASSET_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);
                await _cache.SetHashByKeyAsync(hashKey, hashField, asset);
            }

            _logger.LogTrace($"Get asset by id {command.Id} take {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
            return asset;
        }

        private async Task<GetAssetDto> GetAssetByIdFromDbOptimizedAsync(GetAssetById command)
        {
            Domain.Entity.Asset assetEntity = await _readAssetRepository.FindByIdAsync(command.Id);
            if (assetEntity == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation("AssetId");
            GetAssetDto asset = GetAssetDto.Create(assetEntity);
            var attributeDynamics = asset.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC);
            await DecorateDynamicAttributePayloadAsync(attributeDynamics);
            return asset;
        }

        private async Task<(GetAssetDto asset, string key)> TryGetAssetFromCache(Guid assetId, bool useCache)
        {
            var hashField = CacheKey.ASSET_HASH_FIELD.GetCacheKey(assetId);
            var hashKey = CacheKey.ASSET_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);
            GetAssetDto asset = null;

            if (useCache)
            {
                asset = await _cache.GetHashByKeyAsync<GetAssetDto>(hashKey, hashField);
            }

            return (asset, hashField);
        }

        public async Task<GetAssetDto> FindAssetSnapshotByIdAsync(GetAssetById command, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            var hashField = CacheKey.ASSET_SNAPSHOT_HASH_FIELD.GetCacheKey(command.Id);
            var hashKey = CacheKey.ASSET_SNAPSHOT_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);

            GetAssetDto asset = null;
            if (command.UseCache)
            {
                asset = await _cache.GetHashByKeyAsync<GetAssetDto>(hashKey, hashField);
            }

            if (asset == null)
            {
                asset = await GetAssetSnapshotByIdFromDbAsync(command);
                await _cache.SetHashByKeyAsync(hashKey, hashField, asset);
            }

            _logger.LogTrace($"Get asset snapshot by id {command.Id} take {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
            return asset;
        }

        private async Task<GetAssetDto> GetAssetByIdFromDbAsync(GetAssetById command)
        {
            Domain.Entity.Asset assetEntity = await _readAssetRepository.FindAsync(command.Id);
            if (assetEntity == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation("AssetId");

            GetAssetDto asset = GetAssetDto.Create(assetEntity);

            var attributeDynamics = asset.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC);
            await DecorateDynamicAttributePayloadAsync(attributeDynamics);
            return asset;
        }

        private async Task<GetAssetDto> GetAssetSnapshotByIdFromDbAsync(GetAssetById command)
        {
            Domain.Entity.Asset assetEntity = await _unitOfWork.Assets.FindSnapshotAsync(command.Id);
            if (assetEntity == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation("AssetId");

            GetAssetDto asset = GetAssetDto.Create(assetEntity);

            var attributeDynamics = asset.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC);
            await DecorateDynamicAttributePayloadAsync(attributeDynamics);
            return asset;
        }

        public async Task<GetFullAssetDto> FindFullAssetByIdAsync(GetFullAssetById command, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            var hashField = CacheKey.FULL_ASSET_HASH_FIELD.GetCacheKey(command.Id);
            var hashKey = CacheKey.FULL_ASSET_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);

            GetFullAssetDto asset = null;
            if (command.UseCache)
            {
                asset = await _cache.GetHashByKeyAsync<GetFullAssetDto>(hashKey, hashField);
            }

            if (asset == null)
            {
                asset = await GetFullAssetByIdFromDbAsync(command);
                await _cache.SetHashByKeyAsync(hashKey, hashField, asset);
            }

            _logger.LogTrace($"Get asset by id {command.Id} take {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
            if (command.AuthorizeUserAccess)
            {
                _securityService.AuthorizeAccess(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, Privileges.Asset.Rights.READ_ASSET, asset.ResourcePath, asset.CreatedBy);

                if (command.AuthorizeAssetAttributeAccess)
                {
                    _securityService.AuthorizeAccess(ApplicationInformation.APPLICATION_ID, Privileges.AssetAttribute.ENTITY_NAME, Privileges.AssetAttribute.Rights.READ_ASSET_ATTRIBUTE, asset.ResourcePath, asset.CreatedBy, includeRoleBase: true);
                }
            }

            _securityContext.Authorize(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, Privileges.Asset.Rights.READ_ASSET);
            var restrictedIds = _securityContext.RestrictedIds.Select(Guid.Parse);

            _securityContext.Authorize(ApplicationInformation.APPLICATION_ID, Privileges.AssetAttribute.ENTITY_NAME, Privileges.AssetAttribute.Rights.READ_ASSET_ATTRIBUTE);
            restrictedIds = restrictedIds.Union(_securityContext.RestrictedIds.Select(Guid.Parse)).ToList();
            RemoveRestricted(asset, restrictedIds);
            return await _tagService.FetchTagsAsync(asset);
        }

        private void RemoveRestricted(GetFullAssetDto asset, IEnumerable<Guid> restrictedIds)
        {
            asset.Children.RemoveAll(x => restrictedIds.Contains(x.Id));
            foreach (var child in asset.Children)
                RemoveRestricted(child, restrictedIds);
        }

        private async Task<GetFullAssetDto> GetFullAssetByIdFromDbAsync(GetFullAssetById command)
        {
            Domain.Entity.Asset assetEntity = await _readAssetRepository.FindFullAssetByIdAsync(command.Id);
            if (assetEntity == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation("AssetId");
            GetFullAssetDto asset = GetFullAssetDto.Create(assetEntity);
            await DecorateDynamicAttributePayloadAsync(asset);
            return asset;
        }

        private async Task DecorateDynamicAttributePayloadAsync(GetFullAssetDto asset)
        {
            var attributeDynamics = asset.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC);
            await DecorateDynamicAttributePayloadAsync(attributeDynamics);
            foreach (var child in asset.Children)
            {
                await DecorateDynamicAttributePayloadAsync(child);
            }
        }

        private async Task DecorateDynamicAttributePayloadAsync(IEnumerable<AssetAttributeDto> attributeDynamics)
        {
            if (attributeDynamics.Any())
            {
                var deviceIds = attributeDynamics.Where(x => x.Payload.ContainsKey(AttributePayloadConstants.DEVICE_ID)
                                                                && x.Payload[AttributePayloadConstants.DEVICE_ID] != null)
                                                .Select(x => x.Payload[AttributePayloadConstants.DEVICE_ID]?.ToString()).Distinct();
                var metricKeyMappings = await _unitOfWork.Devices.AsQueryable().Include(x => x.Template)
                                                                                    .ThenInclude(x => x.Payloads)
                                                                                    .ThenInclude(x => x.Details)
                                                                                    .ThenInclude(x => x.TemplateKeyType)
                                        .Where(x => deviceIds.Any(a => a == x.Id))
                                        .ToDictionaryAsync(x => x.Id, y => y.Template.Payloads.SelectMany(x => x.Details));

                foreach (var attribute in attributeDynamics)
                {
                    if (!string.IsNullOrWhiteSpace(attribute.Payload.DeviceId) &&
                        metricKeyMappings.ContainsKey(attribute.Payload.DeviceId))
                    {
                        var metricMapping = metricKeyMappings[attribute.Payload.DeviceId];
                        var templateDetails = metricMapping.Where(x => x.TemplateKeyType.Name == TemplateKeyTypeConstants.METRIC
                                                                                     || x.TemplateKeyType.Name == TemplateKeyTypeConstants.AGGREGATION);
                        attribute.Payload.MetricName = templateDetails.FirstOrDefault(d => d.Key == attribute.Payload.MetricKey)?.Name;
                    }
                }
            }
        }

        public async Task<GetAssetDto> GetAssetCloneAsync(GetAssetClone command, CancellationToken cancellationToken)
        {
            Domain.Entity.Asset cloneAsset = null;
            try
            {
                var asset = await _readAssetRepository.OnlyAssetAsQueryable().Include(x => x.ParentAsset).AsNoTracking().FirstOrDefaultAsync(x => x.Id == command.Id);
                if (asset == null)
                    throw new EntityNotFoundException();

                await _unitOfWork.BeginTransactionAsync();
                cloneAsset = await CloneAssetTreeByIdAsync(asset.ParentAsset, command.Id, command.IncludeChildren);
                await _unitOfWork.Assets.AddEntityAsync(cloneAsset);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET, ActivitiesLogEventAction.Clone, ActionStatus.Fail, payload: command);
                throw;
            }

            await _unitOfWork.Assets.UpdateAssetPathAsync(cloneAsset.Id);
            await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET, ActivitiesLogEventAction.Clone, ActionStatus.Success, cloneAsset.Id, cloneAsset.Name, payload: command);

            var totalAsset = await _unitOfWork.Assets.GetTotalAssetAsync();
            await _dispatcher.SendAsync(AssetChangedEvent.CreateFrom(cloneAsset, totalAsset, _tenantContext));

            return GetAssetDto.Create(cloneAsset);
        }

        protected override async Task RetrieveDataAsync(GetAssetByCriteria criteria, BaseSearchResponse<GetAssetSimpleDto> result)
        {
            var listResult = await _readAssetRepository.GetAssetSimpleAsync(criteria, paging: true);
            if (listResult.Any())
            {
                result.AddRangeData(listResult);
            }
        }

        protected override async Task CountAsync(GetAssetByCriteria criteria, BaseSearchResponse<GetAssetSimpleDto> result)
        {
            result.TotalCount = await _readAssetRepository.CountAsync(criteria);
        }

        public async Task<BaseSearchResponse<GetAssetHierarchyDto>> HierarchySearchAsync(GetAssetHierarchy command, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            var assetHierarchies = await _readAssetRepository.HierarchySearchAsync(command.AssetName, command.TagIds);
            var assets = assetHierarchies.Select(GetAssetHierarchyDto.Create).OrderBy(x => x.RootAssetCreatedUtc).ThenBy(x => x.CreatedUtc);
            var totalMilliseconds = (long)(DateTime.UtcNow - start).TotalMilliseconds;
            return new BaseSearchResponse<GetAssetHierarchyDto>(totalMilliseconds, assets.Count(), command.PageSize, command.PageIndex, assets);
        }

        public async Task<UpsertAssetDto> UpsertAssetAsync(UpsertAsset command, CancellationToken cancellationToken)
        {
            JsonPatchDocument document = command.Data;
            List<AssetTracking> trackingAssets = new List<AssetTracking>();
            List<Operation> operations = document.Operations;
            UpsertAssetDto result = new UpsertAssetDto();
            bool clearCache = false;
            bool onlyClearAssetDetailCache = false;
            var resultModels = new List<SharedKernel.BaseJsonPathDocument>();
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                foreach (Operation operation in operations)
                {
                    SharedKernel.BaseJsonPathDocument resultModel;
                    (clearCache, onlyClearAssetDetailCache, resultModel) = await ProcessUpsertAssetOperationAsync(operation, trackingAssets, cancellationToken);
                    resultModels.Add(resultModel);
                }
                result.Data = resultModels;
                await _unitOfWork.CommitAsync();
            }
            catch (System.Exception exc)
            {
                await HandleUpsertAssetExceptionAsync(exc, trackingAssets, command);
                throw;
            }

            await PostProcessUpsertAssetAsync(trackingAssets, clearCache, onlyClearAssetDetailCache, command);
            return result;
        }

        private async Task<(
            bool clearCache,
            bool onlyClearAssetDetailCache,
            SharedKernel.BaseJsonPathDocument resultModel
        )> ProcessUpsertAssetOperationAsync(Operation operation, List<AssetTracking> trackingAssets, CancellationToken cancellationToken)
        {
            bool clearCache = false;
            bool onlyClearAssetDetailCache = false;
            string path;
            var resultModel = new SharedKernel.BaseJsonPathDocument
            {
                OP = operation.op,
                Path = operation.path
            };
            switch (operation.op)
            {
                case "add":
                    path = operation.path.Replace("/", "");
                    var addAssetDto = JObject.FromObject(operation.value).ToObject<AddAsset>();
                    if (Guid.TryParse(path, out var parentAssetId))
                    {
                        //if elementID null => add in root
                        addAssetDto.ParentAssetId = parentAssetId;
                    }
                    var trackingCreate = new AssetTracking { AssetName = addAssetDto.Name, ParentAssetId = addAssetDto.ParentAssetId, ActionType = ActionTypeEnum.Created };
                    trackingAssets.Add(trackingCreate);
                    var addResult = await ProcessAddAsync(addAssetDto, cancellationToken);
                    trackingCreate.AssetId = addResult.Id;
                    trackingCreate.ResourcePath = addResult.ResourcePath;
                    resultModel.Values = addResult;
                    clearCache = true;
                    break;

                case "edit":
                    path = operation.path.Replace("/", "");
                    if (Guid.TryParse(path, out var assetId))
                    {
                        var updateAsset = JObject.FromObject(operation.value).ToObject<UpdateAsset>();
                        updateAsset.Id = assetId;
                        var trackingUpdate = new AssetTracking { AssetId = updateAsset.Id, AssetName = updateAsset.Name, ParentAssetId = updateAsset.ParentAssetId, ActionType = ActionTypeEnum.Updated };
                        trackingAssets.Add(trackingUpdate);
                        var updateAssetDto = await ProcessUpdateAsync(updateAsset, cancellationToken);
                        trackingUpdate.ResourcePath = updateAssetDto.ResourcePath;
                        clearCache = true;
                        resultModel.Values = updateAssetDto;
                    }
                    break;

                case "edit_parent":
                    path = operation.path.Replace("/", "");
                    if (Guid.TryParse(path, out var editAssetId))
                    {
                        var updateAsset = JObject.FromObject(operation.value).ToObject<UpdateAsset>();
                        updateAsset.Id = editAssetId;
                        var trackEditParent = new AssetTracking { AssetId = updateAsset.Id, AssetName = updateAsset.Name, ParentAssetId = updateAsset.ParentAssetId, ActionType = ActionTypeEnum.Updated };
                        trackingAssets.Add(trackEditParent);
                        var editParent = await ChangeAssetParentAsync(updateAsset, cancellationToken);
                        trackEditParent.ResourcePath = editParent.ResourcePath;
                        trackEditParent.AssetName = editParent.Name;
                        resultModel.Values = editParent;
                        onlyClearAssetDetailCache = true;
                    }
                    break;

                case "remove":
                    BaseResponse baseResponse = BaseResponse.Failed;
                    path = operation.path.Replace("/", "");
                    if (Guid.TryParse(path, out var _))
                    {
                        var deleteAsset = JObject.FromObject(operation.value).ToObject<DeleteAsset>();

                        var entityTracking = await _unitOfWork.Assets.FindAsync(deleteAsset.Id);
                        if (entityTracking == null)
                            throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);

                        deleteAsset.Asset = entityTracking;

                        var trackDelete = new AssetTracking { AssetId = deleteAsset.Id, ActionType = ActionTypeEnum.Deleted, DeleteTable = deleteAsset.WithTable, DeleteMedia = deleteAsset.WithMedia, AssetName = entityTracking.Name, ResourcePath = entityTracking.ResourcePath };
                        trackingAssets.Add(trackDelete);
                        (baseResponse, _, _) = await ProcessRemoveAsync(deleteAsset, cancellationToken);
                        clearCache = true;
                    }
                    resultModel.Values = baseResponse;
                    break;
            }
            return (clearCache, onlyClearAssetDetailCache, resultModel);
        }

        private async Task HandleUpsertAssetExceptionAsync(System.Exception exc, List<AssetTracking> trackingAssets, UpsertAsset command)
        {
            _logger.LogError(exc, exc.Message);
            await _unitOfWork.RollbackAsync();
            ActionType action = trackingAssets.Select(x => x.ActionType switch
            {
                ActionTypeEnum.Created => ActionType.Add,
                ActionTypeEnum.Updated => ActionType.Update,
                _ => ActionType.Delete
            }).FirstOrDefault();

            IDictionary<string, object> payload = new Dictionary<string, object>();
            if (exc is EntityValidationException validateException)
            {
                if (validateException.Payload != null)
                {
                    payload = JObject.FromObject(validateException.Payload).ToObject<Dictionary<string, object>>();
                }
                if (validateException.Failures.Any())
                {
                    payload["ErrorMessage"] = validateException.Failures.First().Value.FirstOrDefault();
                }
            }
            else if (exc is BaseException baseException)
            {
                payload["ErrorMessage"] = baseException.Message;
                payload["ErrorCode"] = baseException.ErrorCode;
                payload["DetailCode"] = baseException.DetailCode;
            }
            if (payload.Any())
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET, action, ActionStatus.Fail, trackingAssets.Select(x => x.AssetId), trackingAssets.Select(x => x.AssetName), command.Data, payload);
            }
            else
                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET, action, ActionStatus.Fail, trackingAssets.Select(x => x.AssetId), trackingAssets.Select(x => x.AssetName), command.Data);
        }

        private async Task PostProcessUpsertAssetAsync(List<AssetTracking> trackingAssets, bool clearCache, bool onlyClearAssetDetailCache, UpsertAsset command)
        {
            var assetIds = trackingAssets.Select(x => x.AssetId).ToList();
            foreach (var assetId in assetIds)
            {
                // update  resource path
                // need to run in sequential
                await _unitOfWork.Assets.UpdateAssetPathAsync(assetId);
            }

            if (clearCache || onlyClearAssetDetailCache)
            {
                await _deviceBackgroundService.QueueAsync(_tenantContext, new CleanAssetCache() { OnlyCleanAssetDetail = onlyClearAssetDetailCache });
            }

            // this query causing connection leak.
            // changing to use dapper for the query

            var totalAsset = await _unitOfWork.Assets.GetTotalAssetAsync();
            var tasks = new List<Task>();
            foreach (var asset in trackingAssets)
            {
                tasks.AddRange(new[]{
                        _dispatcher.SendAsync(new AssetChangedEvent(asset.AssetId, asset.ParentAssetId, asset.AssetName, asset.ResourcePath, asset.DeleteTable, asset.DeleteMedia, totalAsset,_tenantContext, asset.ActionType)),
                        NotifyAssetChangeAsync(asset.AssetId, asset.ActionType)
                       });
            }

            //send log
            ActionType action = trackingAssets.Select(x => x.ActionType switch
            {
                ActionTypeEnum.Created => ActionType.Add,
                ActionTypeEnum.Updated => ActionType.Update,
                _ => ActionType.Delete
            }).FirstOrDefault();
            tasks.Add(_auditLogService.SendLogAsync(ActivityEntityAction.ASSET, action, ActionStatus.Success, trackingAssets.Select(x => x.AssetId), trackingAssets.Select(x => x.AssetName), command.Data));
            await Task.WhenAll(tasks);
        }

        private async Task NotifyAssetChangeAsync(Guid assetId, ActionTypeEnum actionType)
        {
            //add asset changed
            if (actionType == ActionTypeEnum.Deleted)
            {
                var notification = new EntityChangedItem(EntityType.Asset, assetId, null, EntityChangedAction.Delete, _userContext.Upn);
                var message = new AssetListNotificationMessage(_tenantContext.ProjectId, notification);
                await _notificationService.SendAssetListNotifyAsync(message);
            }
        }

        public async Task<SendConfigurationResultDto> SendConfigurationToDeviceIotAsync(SendConfigurationToDeviceIot request, CancellationToken cancellationToken, bool rowVersionCheck = true)
        {
            // need to get from the service, it's contains all information
            var asset = await FindAssetByIdAsync(new GetAssetById(request.AssetId), cancellationToken);
            if (asset == null)
            {
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(request.AssetId));
            }

            var commandAttribute = asset.Attributes.FirstOrDefault(x => x.Id == request.AttributeId);
            if (commandAttribute == null)
            {
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(request.AttributeId));
            }

            // checking the row version.
            // should be matched with latest in database
            if (rowVersionCheck && request.RowVersion != commandAttribute.Payload.RowVersion)
            {
                throw EntityValidationExceptionHelper.GenerateException(nameof(request.RowVersion), MessageDetailConstants.TOO_MANY_REQUEST, detailCode: MessageDetailConstants.TOO_MANY_REQUEST);
            }

            var deviceId = string.IsNullOrEmpty(request.DeviceId) ? commandAttribute.Payload.DeviceId : request.DeviceId;
            var metricKey = string.IsNullOrEmpty(request.MetricKey) ? commandAttribute.Payload.MetricKey : request.MetricKey;
            var device = await _deviceService.FindByIdAsync(new GetDeviceById(deviceId), cancellationToken);
            if (device == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation("DeviceId");

            var bindingTemplate = device.Template.Bindings.FirstOrDefault(x => x.Key == metricKey);
            if (bindingTemplate == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation("MetricKey");

            var metrics = new List<CloudToDeviceMessage> {
                new CloudToDeviceMessage
                {
                    Key = bindingTemplate.Key,
                    Value = request.Value?.ToString(),
                    DataType = bindingTemplate.DataType
                }
            };

            var output = new SendConfigurationResultDto(false, request.RowVersion);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var newRowVersion = await _unitOfWork.AssetAttributes.TrackCommandHistoryAsync(commandAttribute.Id, deviceId, metricKey, commandAttribute.Payload.RowVersion.Value, request.Value?.ToString());
                var pushMessage = new PushMessageToDevice(deviceId, metrics);
                var result = await _deviceService.PushConfigurationMessageAsync(pushMessage, cancellationToken);

                if (result.IsSuccess)
                {
                    await _unitOfWork.CommitAsync();
                    output = new SendConfigurationResultDto(true, newRowVersion);
                }
                else
                {
                    await _unitOfWork.RollbackAsync();
                }
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            // clear the asset cache.
            if (output.IsSuccess)
                await ClearCacheAndReloadAssetInformationAsync(request.AssetId);

            return output;
        }

        public async Task<AttributeCommandDto> SendConfigurationToDeviceIotMutipleAsync(IEnumerable<IGrouping<Guid, SendConfigurationToDeviceIot>> assets, CancellationToken cancellationToken, bool rowVersionCheck = true)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var attributes = new List<AttributeCommandDetailDto>();
                var pushMessages = new List<PushMessageToDevice>();
                foreach (var asset in assets)
                {
                    // need to get from the service, it's contains all information
                    var assetDto = await FindAssetByIdAsync(new GetAssetById(asset.Key), cancellationToken);
                    if (assetDto == null)
                    {
                        throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(asset.Key));
                    }
                    foreach (var item in asset)
                    {
                        await PutMessagesAndAttributesAsync(item, assetDto, attributes, pushMessages, rowVersionCheck);
                    }
                }

                //send message
                var result = await _deviceService.PushConfigurationMessageMutipleAsync(pushMessages, cancellationToken);
                if (result.IsSuccess)
                {
                    await _unitOfWork.CommitAsync();
                    // clear the asset cache.
                    foreach (var item in assets)
                    {
                        await ClearCacheAndReloadAssetInformationAsync(item.Key);
                    }
                    return new AttributeCommandDto(_tenantContext.TenantId, _tenantContext.SubscriptionId, _tenantContext.ProjectId, attributes);
                }
                else
                {
                    throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_SEND_CONFIGURATION_FAIL);
                }
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private async Task PutMessagesAndAttributesAsync(SendConfigurationToDeviceIot item,
                                                         GetAssetDto assetDto,
                                                         List<AttributeCommandDetailDto> attributes,
                                                         List<PushMessageToDevice> pushMessages,
                                                         bool rowVersionCheck)
        {
            var commandAttribute = assetDto.Attributes.FirstOrDefault(x => x.Id == item.AttributeId);
            if (commandAttribute == null)
            {
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(item.AttributeId));
            }

            var duplicatedAttribute = attributes.FirstOrDefault(x => x.AttributeId == item.AttributeId);
            if (duplicatedAttribute != null)
            {
                duplicatedAttribute.RowVersion = await _unitOfWork.AssetAttributes.TrackCommandHistoryAsync(commandAttribute.Id, item.DeviceId, item.MetricKey, duplicatedAttribute.RowVersion, item.Value?.ToString());
                var newMetric = pushMessages.FirstOrDefault(x => x.Id == item.DeviceId).Metrics.FirstOrDefault(x => x.Key == item.MetricKey);
                newMetric.Value = item.Value?.ToString();
            }
            else
            {
                // should be matched with latest in database
                if (rowVersionCheck && item.RowVersion != commandAttribute.Payload.RowVersion)
                {
                    throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(item.RowVersion));
                }
                var attribute = new AttributeCommandDetailDto
                {
                    AssetId = item.AssetId,
                    AttributeId = item.AttributeId,
                    RowVersion = await _unitOfWork.AssetAttributes.TrackCommandHistoryAsync(commandAttribute.Id, item.DeviceId, item.MetricKey,
                                                                                            commandAttribute.Payload.RowVersion.Value, item.Value?.ToString()),
                    DeviceId = item.DeviceId,
                };
                attributes.Add(attribute);

                var deviceMessage = pushMessages.FirstOrDefault(x => x.Id == item.DeviceId);
                if (deviceMessage != null)
                {
                    var metrics = deviceMessage.Metrics.ToList();
                    metrics.Add(new CloudToDeviceMessage
                    {
                        Key = item.MetricKey,
                        Value = item.Value?.ToString(),
                    });
                    deviceMessage.Metrics = metrics;
                }
                else
                {
                    var metrics = new List<CloudToDeviceMessage> {
                        new CloudToDeviceMessage
                        {
                            Key = item.MetricKey,
                            Value = item.Value?.ToString(),
                        }
                    };
                    var pushMessage = new PushMessageToDevice(item.DeviceId, metrics);
                    pushMessages.Add(pushMessage);
                }
            }
        }

        private async Task ClearCacheAndReloadAssetInformationAsync(Guid assetId)
        {
            var hashKey = CacheKey.ASSET_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);
            List<string> hashFields = await _cache.GetHashFieldsByKeyAsync(hashKey);

            if (hashFields == null || hashFields.Count == 0)
            {
                return;
            }

            hashFields = hashFields.Where(f => f.Contains(assetId.ToString())).ToList();

            if (hashFields.Count == 0)
            {
                return;
            }

            await _cache.DeleteHashByKeysAsync(hashKey, hashFields);
        }

        private async Task<GetAssetSimpleDto> ProcessAddAsync(AddAsset command, CancellationToken cancellationToken)
        {
            //add asset one by one and add attribute + add attributeChangedEvents =>> call to attribute service
            // validation input.
            await _dynamicValidator.ValidateAsync(command, cancellationToken);
            var asset = new Domain.Entity.Asset
            {
                Id = command.Id,
                Name = command.Name,
                AssetTemplateId = command.AssetTemplateId,
                ParentAssetId = command.ParentAssetId,
                RetentionDays = command.RetentionDays ?? 90,
                CreatedBy = _userContext.Upn,
                IsDocument = command.IsDocument
            };
            await ValidateDuplicateNameAsync(command.ParentAssetId, asset.Id, asset.Name, _userContext.Upn);
            var deviceIds = command.Mappings.Where(x => !string.IsNullOrEmpty(x.DeviceId) && !x.ContainsKey("integrationId"));
            await ValidateDeletedDevicesAsync(deviceIds, nameof(command.Mappings));
            if (asset.AssetTemplateId.HasValue)
            {
                var template = await _readAssetTemplateRepository.FindAsync(asset.AssetTemplateId.Value);
                await ValidateAssetTemplateAsync(command, asset, template, deviceIds);
            }

            var tagIds = Array.Empty<long>();
            command.Upn = _userContext.Upn;
            command.ApplicationId = Guid.Parse(_userContext.ApplicationId ?? ApplicationInformation.APPLICATION_ID);
            if (command.Tags != null && command.Tags.Any())
            {
                tagIds = await _tagService.UpsertTagsAsync(command);
            }
            var entityId = EntityTagHelper.GetEntityId(asset.Id);
            asset.EntityTags = EntityTagHelper.GetEntityTags(EntityTypeConstants.ASSET, tagIds, entityId);
            var result = await _unitOfWork.Assets.AddEntityAsync(asset);

            // standalone asset
            foreach (var attribute in command.Attributes)
            {
                attribute.AssetId = asset.Id;
                attribute.DecimalPlace = attribute.DataType == DataTypeConstants.TYPE_DOUBLE ? attribute.DecimalPlace : null;
                await _assetAttributeHandler.AddAsync(attribute, command.Attributes, cancellationToken);
            }

            // az: https://dev.azure.com/thanhtrungbui/yokogawa-ppm/_workitems/edit/14226
            // add children asset
            var children = new List<GetAssetSimpleDto>();
            foreach (var child in command.Children)
            {
                child.ParentAssetId = result.Id;
                var childDto = await ProcessAddAsync(child, cancellationToken);
                children.Add(childDto);
            }

            return new GetAssetSimpleDto() { Id = result.Id, Name = result.Name, ParentAssetId = result.ParentAssetId, AssetTemplateId = result.AssetTemplateId, Attributes = result.Attributes.Select(AssetAttributeDto.Create), Children = children };
        }

        private async Task ValidateAssetTemplateAsync(AddAsset command, Domain.Entity.Asset asset,
                                                      Domain.Entity.AssetTemplate template,
                                                      IEnumerable<AttributeMapping> deviceIds)
        {
            if (template == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.Asset.AssetTemplateId));
            //validate request command must have all mappings for all dynamic & integration type template attributes
            if (template.Attributes.Any())
            {
                await ValidateDeviceMappingAsync(template.Attributes, deviceIds);

                // skip the runtime attribute with disable expression
                var missingAttributes = from templateAttribute in template.Attributes
                                        join mappingAttribute in command.Mappings on templateAttribute.Id equals mappingAttribute.TemplateAttributeId into gj
                                        from g in gj.DefaultIfEmpty()
                                        where supportedAttributeTypes.Contains(templateAttribute.AttributeType) && g == null // filter dynamic/integration which does not have mapping
                                        select 1;
                if (missingAttributes.Any())
                {
                    // mapping is not complete
                    throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(AddAsset.Mappings));
                }
            }

            // need to add the mapping
            var mappingAttributes = template.Attributes.ToDictionary(x => x.Id, x => Guid.NewGuid()); // mapping template attribute id & new asset attribute id
            foreach (var templateAttribute in template.Attributes.Where(x => x.AttributeType != AttributeTypeConstants.TYPE_RUNTIME).OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber))
            {
                var mapping = command.Mappings.FirstOrDefault(x => x.TemplateAttributeId == templateAttribute.Id);
                await _attributeMappingHandler.DecorateAssetBasedOnTemplateAsync(asset, templateAttribute, mappingAttributes, mapping);
            }
            foreach (var runtimeMapping in template.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_RUNTIME).OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber))
            {
                var mapping = command.Mappings.FirstOrDefault(x => x.TemplateAttributeId == runtimeMapping.Id);
                await _attributeMappingHandler.DecorateAssetBasedOnTemplateAsync(asset, runtimeMapping, mappingAttributes, mapping);
            }
        }

        private async Task ValidateDeletedDevicesAsync(IEnumerable<AttributeMapping> deviceIds, string mappingsFieldName)
        {
            if (deviceIds.Any())
            {
                var deletedDeviceIds = await _unitOfWork.Devices.ValidateDeviceIdsAsync(deviceIds.Select(x => x.DeviceId).ToArray());
                if (deletedDeviceIds.Any())
                {
                    var payload = new Dictionary<string, object>
                    {
                        {"DeletedIds", deletedDeviceIds}
                    };
                    throw ValidationExceptionHelper.GenerateNotFoundValidation(mappingsFieldName, payload);
                }
            }
        }

        private async Task<GetAssetSimpleDto> ChangeAssetParentAsync(UpdateAsset command, CancellationToken cancellationToken)
        {
            await ValidateIfAssetLockedAsync(command.Id, cancellationToken);

            var assetTracking = await _unitOfWork.Assets.AsQueryable().FirstOrDefaultAsync(x => x.Id == command.Id);
            await ValidateDuplicateNameAsync(command.ParentAssetId, command.Id, assetTracking.Name, assetTracking.CreatedBy);
            assetTracking.ParentAssetId = command.ParentAssetId;
            await _unitOfWork.Assets.UpdateEntityAsync(assetTracking);
            await HandleRedisCacheWhenEntityChangedAsync(assetTracking);
            var result = new GetAssetSimpleDto() { Id = assetTracking.Id, Name = assetTracking.Name, ParentAssetId = assetTracking.ParentAssetId, AssetTemplateId = assetTracking.AssetTemplateId, ResourcePath = assetTracking.ResourcePath };
            return result;
        }

        private async Task<bool> IsDuplicateNameAsync(Guid? parentAssetId, Guid assetId, string name, string createdBy)
        {
            return await _readAssetRepository.OnlyAssetAsQueryable().AsNoTracking().AnyAsync(x =>
                                                x.Name.ToLower() == name.ToLower()
                                                && x.Id != assetId
                                                && x.CreatedBy == createdBy
                                                && x.ParentAssetId == parentAssetId);
        }

        private async Task ValidateDuplicateNameAsync(Guid? parentAssetId, Guid assetId, string name, string createdBy)
        {
            var assetExistWithSameName = await IsDuplicateNameAsync(parentAssetId, assetId, name, createdBy);
            if (assetExistWithSameName)
                throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(Domain.Entity.Asset.Name));
        }

        private async Task<GetAssetSimpleDto> ProcessUpdateAsync(UpdateAsset command, CancellationToken cancellationToken)
        {
            await ValidateIfAssetLockedAsync(command.Id, cancellationToken);

            //check exist element
            var assetTracking = await _unitOfWork.Assets.FindAsync(command.Id);
            if (assetTracking == null)
                throw new EntityNotFoundException();

            if (command.ParentAssetId.HasValue && !await _unitOfWork.Assets.ValidateParentExistedAsync(command.ParentAssetId.Value))
                throw new EntityNotFoundException(detailCode: MessageConstants.ASSET_PARENT_UNSAVED);

            //validation input data.
            await _dynamicValidator.ValidateAsync(command, cancellationToken);

            //validate from DB => just level 1 check with lv2 before need add first and check after
            //validate for duplicated name in that lvl
            await ValidateDuplicateNameAsync(command.ParentAssetId, command.Id, command.Name, assetTracking.CreatedBy);

            var mappingIds = assetTracking.AssetAttributeDynamicMappings.Select(x => x.Id).ToList();
            mappingIds.AddRange(assetTracking.AssetAttributeIntegrationMappings.Select(x => x.Id));
            mappingIds.AddRange(assetTracking.AssetAttributeRuntimeMappings.Select(x => x.Id));
            mappingIds.AddRange(assetTracking.AssetAttributeStaticMappings.Select(x => x.Id));

            if (await AttributeUsedByOtherAttributeAsync(mappingIds, command, assetTracking.AssetTemplateId))
                throw new EntityInvalidException(detailCode: MessageConstants.ASSET_ATTRIBUTE_USING);

            var hasAlias = _unitOfWork.Alias.AsQueryable().Any(x => mappingIds.Contains(x.AliasAttributeId ?? Guid.Empty));

            //not validate for integration attribute
            var deviceIds = command.Mappings.Where(x => !string.IsNullOrEmpty(x.DeviceId) && !x.ContainsKey("integrationId"));
            await ValidateDeletedDevicesAsync(deviceIds, nameof(command.Mappings));

            if (command.AssetTemplateId.HasValue)
            {
                var template = await _readAssetTemplateRepository.FindAsync(command.AssetTemplateId.Value);
                await ValidateAssetTemplateAsync(command, assetTracking, template, deviceIds, hasAlias);
            }
            else
            {
                if (hasAlias)
                    throw new EntityInvalidException(detailCode: MessageConstants.ASSET_USING);

                assetTracking.AssetAttributeStaticMappings.Clear();
                assetTracking.AssetAttributeDynamicMappings.Clear();
                assetTracking.AssetAttributeIntegrationMappings.Clear();
                assetTracking.AssetAttributeRuntimeMappings.Clear();
                assetTracking.AssetAttributeCommandMappings.Clear();
                assetTracking.AssetAttributeAliasMappings.Clear();
            }

            assetTracking.Name = command.Name;
            assetTracking.ParentAssetId = command.ParentAssetId;
            assetTracking.AssetTemplateId = command.AssetTemplateId;
            assetTracking.UpdatedUtc = DateTime.UtcNow;
            assetTracking.RetentionDays = command.RetentionDays ?? 90;
            assetTracking.IsDocument = command.IsDocument;
            command.Upn = _userContext.Upn;
            command.ApplicationId = Guid.Parse(_userContext.ApplicationId ?? ApplicationInformation.APPLICATION_ID);

            var isSameTag = command.IsSameTags(assetTracking.EntityTags);
            if (!isSameTag)
            {
                await _unitOfWork.SharedEntityTags.RemoveByEntityIdAsync(EntityTypeConstants.ASSET, assetTracking.Id, true);

                var tagIds = await _tagService.UpsertTagsAsync(command);
                if (tagIds.Any())
                {
                    var entityId = EntityTagHelper.GetEntityId(command.Id);
                    var entitiesTags = EntityTagHelper.GetEntityTags(EntityTypeConstants.ASSET, tagIds, entityId).ToArray();
                    await _unitOfWork.EntityTags.AddRangeWithSaveChangeAsync(entitiesTags);
                }
            }

            _ = await _unitOfWork.Assets.UpdateEntityAsync(assetTracking);
            await HandleRedisCacheWhenEntityChangedAsync(assetTracking);
            var result = new GetAssetSimpleDto() { Id = assetTracking.Id, Name = assetTracking.Name, ParentAssetId = assetTracking.ParentAssetId, AssetTemplateId = assetTracking.AssetTemplateId, ResourcePath = assetTracking.ResourcePath };
            return result;
        }

        private async Task ValidateAssetTemplateAsync(UpdateAsset command, Domain.Entity.Asset assetTracking,
                                                      Domain.Entity.AssetTemplate template,
                                                      IEnumerable<AttributeMapping> deviceIds, bool hasAlias)
        {
            if (template == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.Asset.AssetTemplateId));

            //validate request command must have all mappings for all dynamic & integration type template attributes
            if (template.Attributes.Any())
                await ValidateAssetTemplateAttributesAsync(command, assetTracking, template, deviceIds);

            var isChangedAssetTemplate = assetTracking.AssetTemplateId != command.AssetTemplateId;
            if (isChangedAssetTemplate)
            {
                if (hasAlias)
                    throw new EntityInvalidException(detailCode: MessageConstants.ASSET_USING);

                await DecorateAssetBasedOnNewTemplateAsync(command, assetTracking, template);
            }
            else
            {
                await DecorateAssetBasedOnUnchangedTemplateAsync(command, assetTracking, template);
            }
        }

        private async Task DecorateAssetBasedOnNewTemplateAsync(UpdateAsset command, Domain.Entity.Asset assetTracking,
                                                                Domain.Entity.AssetTemplate template)
        {
            assetTracking.AssetAttributeStaticMappings.Clear();
            assetTracking.AssetAttributeDynamicMappings.Clear();
            assetTracking.AssetAttributeIntegrationMappings.Clear();
            assetTracking.AssetAttributeRuntimeMappings.Clear();
            assetTracking.AssetAttributeCommandMappings.Clear();

            var mappingAttributes = template.Attributes.ToDictionary(x => x.Id, x => Guid.NewGuid()); // mapping template attribute id & new asset attribute id
            foreach (var templateAttribute in template.Attributes.Where(x => x.AttributeType != AttributeTypeConstants.TYPE_RUNTIME).OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber))
            {
                var mapping = command.Mappings.FirstOrDefault(x => x.TemplateAttributeId == templateAttribute.Id);
                await _attributeMappingHandler.DecorateAssetBasedOnTemplateAsync(assetTracking, templateAttribute, mappingAttributes, mapping);
            }
            foreach (var runtimeMapping in template.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_RUNTIME).OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber))
            {
                var mapping = command.Mappings.FirstOrDefault(x => x.TemplateAttributeId == runtimeMapping.Id);
                await _attributeMappingHandler.DecorateAssetBasedOnTemplateAsync(assetTracking, runtimeMapping, mappingAttributes, mapping);
            }
        }

        private async Task DecorateAssetBasedOnUnchangedTemplateAsync(UpdateAsset command, Domain.Entity.Asset assetTracking,
                                                                      Domain.Entity.AssetTemplate template)
        {
            bool attributeChanged = false;
            var newDynamicAttributes = from templateAttribute in template.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC)
                                       join mappingAttribute in command.Mappings on templateAttribute.Id equals mappingAttribute.TemplateAttributeId into ga
                                       from a in ga.DefaultIfEmpty()
                                       join adm in assetTracking.AssetAttributeDynamicMappings on JObject.FromObject(a).ToObject<AssetAttributeDynamicMapping>().DeviceId equals adm.DeviceId into gadm
                                       from g in gadm.DefaultIfEmpty()
                                       where g == null
                                       select 1;

            var mappingAttributes = template.Attributes.ToDictionary(x => x.Id, x => Guid.NewGuid()); // mapping template attribute id & new asset attribute id
            if (newDynamicAttributes.Any())
            {
                attributeChanged = true;
                assetTracking.AssetAttributeDynamicMappings.Clear();
                foreach (var templateAttribute in template.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC))
                {
                    var mapping = command.Mappings.FirstOrDefault(x => x.TemplateAttributeId == templateAttribute.Id);
                    await _attributeMappingHandler.DecorateAssetBasedOnTemplateAsync(assetTracking, templateAttribute, mappingAttributes, mapping);
                }
            }

            var newCommandAttributes = from templateAttribute in template.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_COMMAND)
                                       join mappingAttribute in command.Mappings on templateAttribute.Id equals mappingAttribute.TemplateAttributeId into ga
                                       from a in ga.DefaultIfEmpty()
                                       join adm in assetTracking.AssetAttributeCommandMappings on JObject.FromObject(a).ToObject<AssetAttributeCommandMapping>().DeviceId equals adm.DeviceId into gadm
                                       from g in gadm.DefaultIfEmpty()
                                       where g == null
                                       select 1;

            if (newCommandAttributes.Any())
            {
                attributeChanged = true;
                assetTracking.AssetAttributeCommandMappings.Clear();
                foreach (var templateAttribute in template.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_COMMAND))
                {
                    var mapping = command.Mappings.FirstOrDefault(x => x.TemplateAttributeId == templateAttribute.Id);
                    await _attributeMappingHandler.DecorateAssetBasedOnTemplateAsync(assetTracking, templateAttribute, mappingAttributes, mapping);
                }
            }

            var newIntegrations = from templateAttribute in template.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_INTEGRATION)
                                  join mappingAttribute in command.Mappings on templateAttribute.Id equals mappingAttribute.TemplateAttributeId into ga
                                  from a in ga.DefaultIfEmpty()
                                  join aim in assetTracking.AssetAttributeIntegrationMappings
                                  on new { IntegrationId = JObject.FromObject(a).ToObject<AssetAttributeIntegrationMapping>()?.IntegrationId, DeviceId = JObject.FromObject(a).ToObject<AssetAttributeIntegrationMapping>().DeviceId } equals new { IntegrationId = aim.IntegrationId, DeviceId = aim.DeviceId }
                                  into gaim
                                  from g in gaim.DefaultIfEmpty()
                                  where g == null
                                  select 1;

            if (newIntegrations.Any())
            {
                attributeChanged = true;
                assetTracking.AssetAttributeIntegrationMappings.Clear();
                foreach (var templateAttribute in template.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_INTEGRATION))
                {
                    var mapping = command.Mappings.FirstOrDefault(x => x.TemplateAttributeId == templateAttribute.Id);
                    await _attributeMappingHandler.DecorateAssetBasedOnTemplateAsync(assetTracking, templateAttribute, mappingAttributes, mapping);
                }
            }

            var newRuntimeAttributes = from templateAttribute in template.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_RUNTIME)
                                       join mappingAttribute in command.Mappings on templateAttribute.Id equals mappingAttribute.TemplateAttributeId into ga
                                       from a in ga.DefaultIfEmpty()
                                           // join adm in assetTracking.AssetAttributeRuntimeMappings on JObject.FromObject(a).ToObject<AssetAttributeRuntimeMapping>().TriggerAssetId equals adm.TriggerAssetId into gadm
                                           // from g in gadm.DefaultIfEmpty()
                                       where a == null
                                       select 1;

            if (newRuntimeAttributes.Any() || attributeChanged)
            {
                assetTracking.AssetAttributeRuntimeMappings.Clear();
                foreach (var runtimeMapping in template.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_RUNTIME).OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber))
                {
                    var mapping = command.Mappings.FirstOrDefault(x => x.TemplateAttributeId == runtimeMapping.Id);
                    await _attributeMappingHandler.DecorateAssetBasedOnTemplateAsync(assetTracking, runtimeMapping, mappingAttributes, mapping);
                }
            }
        }

        private async Task ValidateAssetTemplateAttributesAsync(UpdateAsset command, Domain.Entity.Asset assetTracking,
                                                                Domain.Entity.AssetTemplate template,
                                                                IEnumerable<AttributeMapping> deviceIds)
        {
            await ValidateDeviceMappingAsync(template.Attributes, deviceIds);

            var assetAttributeNames = new List<string>();
            foreach (var attr in assetTracking.Attributes)
            {
                assetAttributeNames.Add(attr.Name);
            }
            var existAttributes = template.Attributes.Where(x => assetAttributeNames.Any(a => a == x.Name)).Select(x => x.Name);
            if (existAttributes.Any())
            {
                var errorPayload = new Dictionary<string, object>();
                errorPayload["TemplateName"] = template.Name;
                errorPayload["ExistAttributes"] = existAttributes;
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(Domain.Entity.Asset.AssetTemplateId), MessageConstants.ASSET_ATTRIBUTE_NAME_ALREADY_EXISTS, errorPayload);
            }

            var missingAttributes = from templateAttribute in template.Attributes
                                    join mappingAttribute in command.Mappings on templateAttribute.Id equals mappingAttribute.TemplateAttributeId into gj
                                    from g in gj.DefaultIfEmpty()
                                    where supportedAttributeTypes.Contains(templateAttribute.AttributeType) && g == null // filter dynamic/integration which does not have mapping
                                    select 1;
            if (missingAttributes.Any())
            {
                // mapping is not complete
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(UpdateAsset.Mappings));
            }
        }

        private async Task<bool> AttributeUsedByOtherAttributeAsync(IEnumerable<Guid> idMappings, UpdateAsset command, Guid? currentTemplateId)
        {
            if (currentTemplateId == command.AssetTemplateId)
                return false;
            foreach (var idMapping in idMappings)
            {
                var existReferenceAssetAttributes = await _readAssetAttributeRepository.AsQueryable().FirstOrDefaultAsync(x => x.AssetAttributeRuntime.Expression.Contains($"{{{idMapping}}}") && x.AssetId == command.Id);
                if (existReferenceAssetAttributes != null)
                    return true;
            }
            return false;
        }

        private async Task<(BaseResponse, string ResourcePath, string assetName)> ProcessRemoveAsync(DeleteAsset command, CancellationToken cancellationToken)
        {
            await ValidateIfAssetLockedAsync(command.Id, cancellationToken);

            if (command.Asset == null)
            {
                command.Asset = await _unitOfWork.Assets.FindAsync(command.Id);
            }
            var entityTracking = command.Asset;
            if (entityTracking == null)
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);

            var resourcePath = entityTracking.ResourcePath;
            _ = await _unitOfWork.Assets.RemoveEntityAsync(entityTracking);

            var triggerCacheKey = CacheKey.FUNCTION_BLOCK_TRIGGER_KEY.GetCacheKey(entityTracking.Id);
            await _cache.DeleteAsync(triggerCacheKey);

            await HandleRedisCacheWhenEntityChangedAsync(entityTracking);

            await _unitOfWork.EntityTags.RemoveByEntityIdAsync(EntityTypeConstants.ASSET, command.Id, isTracking: true);
            return (BaseResponse.Success, resourcePath, entityTracking.Name);
        }

        private async Task<Domain.Entity.Asset> CloneAssetTreeByIdAsync(Domain.Entity.Asset parentAsset, Guid assetId, bool includeChildren, bool appendCopyToName = true)
        {
            var asset = await _readAssetRepository.FindAsync(assetId);
            var dto = GetAssetDto.Create(asset);
            var name = asset.Name;
            if (appendCopyToName)
            {
                name = await BuildCopyNameAsync(asset);
            }
            var newAsset = new Domain.Entity.Asset
            {
                Name = name,
                AssetTemplateId = dto.AssetTemplateId,
                ParentAssetId = parentAsset?.Id,
                RetentionDays = dto.RetentionDays,
                CreatedBy = _userContext.Upn,
                EntityTags = asset.EntityTags.OrderBy(x => x.Id).Select(x => new EntityTagDb
                {
                    TagId = x.TagId,
                    EntityType = x.EntityType,
                    EntityIdGuid = x.EntityIdGuid
                }).ToList()
            };
            newAsset.ResourcePath = parentAsset == null ? $"objects/{newAsset.Id}"
                : $"{parentAsset.ResourcePath}/children/{newAsset.Id}";

            var mappingAttributes = asset.Attributes.ToDictionary(x => x.Id, x => Guid.NewGuid());
            if (dto.AssetTemplateId.HasValue)
            {
                var template = await _unitOfWork.Templates.FindAsync(dto.AssetTemplateId.Value);
                await DecorateClonedAssetBasedOnTemplateAsync(asset, newAsset, template, dto, mappingAttributes);
            }

            //normal attribute
            foreach (var attribute in asset.Attributes.Where(x => x.AttributeType != AttributeTypeConstants.TYPE_RUNTIME).OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber))
                await ProcessClonedAssetNormalAttributesAsync(dto, attribute, newAsset, mappingAttributes);

            await _unitOfWork.Assets.AddAsync(newAsset);// track this asset into db context

            foreach (var attribute in asset.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_RUNTIME).OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber))
                await ProcessClonedAssetRuntimeAttributesAsync(dto, attribute, newAsset, mappingAttributes);

            if (includeChildren)
            {
                foreach (var child in asset.Children)
                {
                    var childAsset = await CloneAssetTreeByIdAsync(newAsset, child.Id, includeChildren, false);
                    newAsset.Children.Add(childAsset);
                }
            }
            return newAsset;
        }

        private async Task ProcessClonedAssetRuntimeAttributesAsync(GetAssetDto dto, Domain.Entity.AssetAttribute attribute,
                                                                    Domain.Entity.Asset root,
                                                                    IDictionary<Guid, Guid> mappingAttributes)
        {

            var dtoAttribute = dto.Attributes.First(x => x.Id == attribute.Id);
            var newAttribute = JObject.FromObject(dtoAttribute).ToObject<Asset.Command.AssetAttribute>();
            newAttribute.Id = mappingAttributes.ContainsKey(attribute.Id) ? mappingAttributes[attribute.Id] : Guid.NewGuid();
            newAttribute.AssetId = root.Id;
            newAttribute.SequentialNumber = attribute.SequentialNumber;
            newAttribute.CreatedUtc = attribute.CreatedUtc;
            mappingAttributes.TryAdd(attribute.Id, newAttribute.Id);
            var expression = attribute.AssetAttributeRuntime.Expression;
            if (attribute.AssetAttributeRuntime.EnabledExpression)
            {
                // replace the expression
                foreach (var mapping in mappingAttributes)
                {
                    expression = expression.Replace($"{mapping.Key}", $"{mapping.Value}");
                }
                newAttribute.Payload[Expression] = expression;
                if (dtoAttribute.Payload.TriggerAttributeId != null)
                {
                    // need to find the appropriate trigger attributeId
                    var newTriggerAttributeId = mappingAttributes.First(x => x.Key == dtoAttribute.Payload.TriggerAttributeId.Value);
                    newAttribute.Payload[TriggerAttributeId] = newTriggerAttributeId.Value;
                }
            }
            var attributes = dto.Attributes.Select(attr =>
            {
                var result = JObject.FromObject(attr).ToObject<Asset.Command.AssetAttribute>();
                result.Id = mappingAttributes[result.Id];
                return result;
            });
            await _assetAttributeHandler.AddAsync(newAttribute, attributes, CancellationToken.None);
        }

        private async Task ProcessClonedAssetNormalAttributesAsync(GetAssetDto dto, Domain.Entity.AssetAttribute attribute,
                                                                   Domain.Entity.Asset root,
                                                                   IDictionary<Guid, Guid> mappingAttributes)
        {
            var dtoAttribute = dto.Attributes.First(x => x.Id == attribute.Id);
            var newAttribute = JObject.FromObject(dtoAttribute).ToObject<Asset.Command.AssetAttribute>();
            newAttribute.Id = mappingAttributes.ContainsKey(attribute.Id) ? mappingAttributes[attribute.Id] : Guid.NewGuid();
            newAttribute.AssetId = root.Id;
            newAttribute.SequentialNumber = attribute.SequentialNumber;
            newAttribute.CreatedUtc = attribute.CreatedUtc;
            if (attribute.AttributeType == AttributeTypeConstants.TYPE_COMMAND)
            {
                newAttribute.Payload["DeviceId"] = null;
                newAttribute.Payload["MetricKey"] = null;
            }
            mappingAttributes.TryAdd(attribute.Id, newAttribute.Id);
            var attributes = dto.Attributes.Select(attr =>
            {
                var result = JObject.FromObject(attr).ToObject<Asset.Command.AssetAttribute>();
                result.Id = mappingAttributes[result.Id];
                return result;
            });
            await _assetAttributeHandler.AddAsync(newAttribute, attributes, CancellationToken.None);
        }

        private async Task DecorateClonedAssetBasedOnTemplateAsync(Domain.Entity.Asset asset, Domain.Entity.Asset root,
                                                                   Domain.Entity.AssetTemplate template,
                                                                   GetAssetDto dto, IDictionary<Guid, Guid> mappingAttributes)
        {
            var keyValues = asset.AssetAttributeStaticMappings.Select(x => new { x.Id, x.AssetAttributeTemplateId })
                            .Union(asset.AssetAttributeDynamicMappings.Select(x => new { x.Id, x.AssetAttributeTemplateId }))
                            .Union(asset.AssetAttributeIntegrationMappings.Select(x => new { x.Id, x.AssetAttributeTemplateId }))
                            .Union(asset.AssetAttributeRuntimeMappings.Select(x => new { x.Id, x.AssetAttributeTemplateId }))
                            .Union(asset.AssetAttributeCommandMappings.Select(x => new { x.Id, x.AssetAttributeTemplateId }))
                            .Union(asset.AssetAttributeAliasMappings.Select(x => new { x.Id, x.AssetAttributeTemplateId }));

            // need to add the mapping
            foreach (Domain.Entity.AssetAttributeTemplate templateAttribute in template.Attributes)
            {
                mappingAttributes.TryAdd(templateAttribute.Id, Guid.NewGuid());
            }
            foreach (var templateAttribute in template.Attributes.Where(x => x.AttributeType != AttributeTypeConstants.TYPE_RUNTIME).OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber))
            {
                var dtoAttribute = (from m in keyValues
                                    join att in dto.Attributes on m.Id equals att.Id
                                    where m.AssetAttributeTemplateId == templateAttribute.Id
                                    select att).First();
                var newAttributeId = await _attributeMappingHandler.DecorateAssetBasedOnTemplateAsync(root, templateAttribute, mappingAttributes, dtoAttribute.Payload, true);
                mappingAttributes.TryAdd(dtoAttribute.Id, newAttributeId);
            }

            foreach (var assetRuntimeAttribute in template.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_RUNTIME).OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber))
            {
                var dtoAttribute = (from m in keyValues
                                    join att in dto.Attributes on m.Id equals att.Id
                                    where m.AssetAttributeTemplateId == assetRuntimeAttribute.Id
                                    select att).First();

                var newRuntimAttribute = await _attributeMappingHandler.DecorateAssetBasedOnTemplateAsync(root, assetRuntimeAttribute, mappingAttributes, dtoAttribute.Payload, true);
                mappingAttributes.TryAdd(dtoAttribute.Id, newRuntimAttribute);
            }
        }

        private async Task<string> BuildCopyNameAsync(Domain.Entity.Asset asset)
        {
            var targetName = string.Concat(asset.Name, " copy");
            var parentId = asset.ParentAssetId;
            var names = await _readAssetRepository.OnlyAssetAsQueryable().AsNoTracking().Where(x => x.ParentAssetId == parentId && x.Name.StartsWith(targetName) && x.CreatedBy == _userContext.Upn).Select(x => x.Name).ToListAsync();
            if (names.Any())
            {
                targetName = string.Concat(names.OrderBy(x => x.Length).Last(), " copy");
            }

            if (targetName.Length > NameConstants.ASSET_NAME_MAX_LENGTH)
                throw new EntityInvalidException(detailCode: MessageConstants.ASSET_CLONE_ASSET_NAME_MAX_LENGTH);
            return targetName;
        }

        private async Task ValidateIfAssetLockedAsync(Guid assetId, CancellationToken cancellationToken)
        {
            var isLocked = await _entityLockService.ValidateEntityLockedByOtherAsync(new EntityLock.Command.ValidateLockEntityCommand()
            {
                HolderUpn = _userContext.Upn,
                TargetId = assetId
            }, cancellationToken);
            if (isLocked)
            {
                throw LockException.CreateAlreadyLockException();
            }
        }

        public Task<IEnumerable<Guid>> GetAllRelatedAssetIdAsync(Guid command, CancellationToken cancellationToken)
        {
            return _unitOfWork.Assets.GetAllRelatedAssetIdAsync(command);
        }

        public async Task<IEnumerable<GetAssetSimpleDto>> GetAssetsByTemplateIdAsync(GetAssetsByTemplateId command, CancellationToken cancellationToken)
        {
            return await _readAssetRepository.GetAssetsByTemplateIdAsync(command.Id);
        }

        public async Task<IEnumerable<ValidateDeviceBindingDto>> ValidateDeviceBindingAsync(ValidateDeviceBindings command, CancellationToken cancellationToken)
        {
            var result = new List<ValidateDeviceBindingDto>();
            foreach (var validate in command.ValidateBindings)
            {
                bool isBindingValid = await ValidateDeviceBindingAsync(validate);
                bool isExistInList = command.ValidateBindings.Select(x => new { DeviceId = x.DeviceId, MetricKey = x.MetricKey }).Count(x => x.MetricKey == validate.MetricKey && x.DeviceId == validate.DeviceId) > 1;
                result.Add(new ValidateDeviceBindingDto() { CommandAttributeId = validate.CommandAttributeId, IsValid = isBindingValid && !isExistInList });
            }

            return result;
        }

        private async Task<bool> ValidateDeviceBindingAsync(ValidateDeviceBinding request)
        {
            if (string.IsNullOrEmpty(request.DeviceId))
            {
                var deviceTemplate = await _readDeviceTemplateRepository.AsQueryable().FirstOrDefaultAsync(x => x.Id == request.DeviceTemplateId);
                if (deviceTemplate == null || deviceTemplate.Deleted)
                    throw new EntityNotFoundException(MessageConstants.DEVICE_TEMPLATE_NOT_FOUND);

                var relatedDeviceIds = await _readDeviceRepository.AsQueryable().AsNoTracking()
                                    .Where(x => x.TemplateId == request.DeviceTemplateId).Select(x => x.Id).ToListAsync();
                bool isExistAttribute = await _readAssetAttributeRepository.AsQueryable()
                                        .AnyAsync(x => relatedDeviceIds.Contains(x.AssetAttributeCommand.DeviceId)
                                                && x.AssetAttributeCommand.MetricKey == request.MetricKey && x.Id != request.CommandAttributeId);
                bool isExistTemplateAttribute = deviceTemplate.AssetAttributeCommandTemplates.Any(x => x.MetricKey == request.MetricKey);
                return !(isExistAttribute || isExistTemplateAttribute);
            }
            else
            {
                bool isExistAttribute = await _readAssetAttributeRepository.AsQueryable()
                                .AnyAsync(x => x.AssetAttributeCommand.DeviceId == request.DeviceId && x.AssetAttributeCommand.MetricKey == request.MetricKey && x.Id != request.CommandAttributeId);
                bool isExistMapping = await _readAssetRepository.OnlyAssetAsQueryable()
                                .SelectMany(x => x.AssetAttributeCommandMappings).AnyAsync(x => x.DeviceId == request.DeviceId
                                        && x.MetricKey == request.MetricKey
                                        && x.Id != request.CommandAttributeId && x.AssetId != request.AssetId);
                return !(isExistAttribute || isExistMapping);
            }
        }

        public async Task<IEnumerable<GetAssetSimpleDto>> GetAssetChildrenAsync(GetAssetChildren command, CancellationToken cancellationToken)
        {
            /* As new approach from Object Base implementation, if an user does not have Full Access, we will return all child assets (which have been provided access via Access Control) with the same level of root assets.
            => So, these child assets should be exclude from Asset's child to avoid confusing.
            */
            var filterList = new List<SearchFilter>
            {
                new SearchFilter(
                    queryKey: "parentAssetId",
                    queryValue: command.AssetId.ToString(),
                    queryType: "guid",
                    operation: "eq"
                )
            };
            if (_securityContext.RestrictedIds != null && _securityContext.RestrictedIds.Any())
            {
                filterList.Add(new SearchFilter(
                    queryKey: "id",
                    queryType: "guid",
                    operation: "nin",
                    queryValue: $"[{string.Join(',', _securityContext.RestrictedIds)}]"
                ));
            }
            if (!_securityContext.FullAccess)
            {
                if (_securityContext.AllowedIds != null && _securityContext.AllowedIds.Any())
                {
                    // Also exclude child asset which has been returned from HierarchySearchWithSecurityAsync
                    filterList.Add(new SearchFilter(
                        queryKey: "id",
                        queryType: "guid",
                        operation: "nin",
                        queryValue: $"[{string.Join(',', _securityContext.AllowedIds)}]"
                    ));
                }
            }
            var filterStr = JsonConvert.SerializeObject(new { and = filterList });
            var criteria = new GetAssetByCriteria
            {
                Filter = filterStr,
                Sorts = "createdUtc=asc"
            };
            var assets = (await _readAssetRepository.GetAssetSimpleAsync(criteria, paging: false)).ToList();
            return assets;
        }

        public async Task<IEnumerable<ArchiveAssetDto>> ArchiveAsync(ArchiveAsset command, CancellationToken cancellationToken)
        {
            var baseQuery = _readAssetRepository.OnlyAssetAsQueryable().Where(x => x.UpdatedUtc <= command.ArchiveTime);

            var assets = await baseQuery.ToListAsync();

            //TODO: why do we call LoadAsync? Does it do some updates to DB?
            await baseQuery.Include(x => x.Triggers).SelectMany(x => x.Triggers).LoadAsync();

            await baseQuery.Include(x => x.Attributes).SelectMany(x => x.Attributes).LoadAsync();
            await baseQuery.Include(x => x.Attributes).ThenInclude(x => x.AssetAttributeAlias).SelectMany(x => x.Attributes).Select(x => x.AssetAttributeAlias).LoadAsync();
            await baseQuery.Include(x => x.Attributes).ThenInclude(x => x.AssetAttributeCommand).ThenInclude(x => x.Device).ThenInclude(x => x.Template).ThenInclude(x => x.Bindings).SelectMany(x => x.Attributes).Select(x => x.AssetAttributeCommand).LoadAsync();
            await baseQuery.Include(x => x.Attributes).ThenInclude(x => x.AssetAttributeDynamic).SelectMany(x => x.Attributes).Select(x => x.AssetAttributeDynamic).LoadAsync();
            await baseQuery.Include(x => x.Attributes).ThenInclude(x => x.AssetAttributeIntegration).SelectMany(x => x.Attributes).Select(x => x.AssetAttributeIntegration).LoadAsync();
            await baseQuery.Include(x => x.Attributes).ThenInclude(x => x.AssetAttributeRuntime).ThenInclude(x => x.Triggers).SelectMany(x => x.Attributes).Select(x => x.AssetAttributeRuntime).LoadAsync();

            await baseQuery.Include(x => x.AssetAttributeAliasMappings).SelectMany(x => x.AssetAttributeAliasMappings).LoadAsync();
            await baseQuery.Include(x => x.AssetAttributeCommandMappings).SelectMany(x => x.AssetAttributeCommandMappings).LoadAsync();
            await baseQuery.Include(x => x.AssetAttributeDynamicMappings).SelectMany(x => x.AssetAttributeDynamicMappings).LoadAsync();
            await baseQuery.Include(x => x.AssetAttributeIntegrationMappings).SelectMany(x => x.AssetAttributeIntegrationMappings).LoadAsync();
            await baseQuery.Include(x => x.AssetAttributeStaticMappings).SelectMany(x => x.AssetAttributeStaticMappings).LoadAsync();
            await baseQuery.Include(x => x.AssetAttributeRuntimeMappings).SelectMany(x => x.AssetAttributeRuntimeMappings).LoadAsync();

            return assets.Select(ArchiveAssetDto.CreateDto);
        }

        public async Task<BaseResponse> VerifyArchiveAsync(VerifyArchivedAsset command, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<ArchiveAssetDataDto>(command.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
            foreach (var asset in data.Assets)
            {
                var validation = await _assetVerifyValidator.ValidateAsync(asset);
                if (!validation.IsValid)
                {
                    throw EntityValidationExceptionHelper.GenerateException(nameof(command.Data), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
                }
            }
            return BaseResponse.Success;
        }

        public async Task<ValidateAssetResponse> ValidateDependencyAssetAsync(ValidateAsset command, CancellationToken cancellationToken)
        {
            var asset = await FindAssetByIdAsync(new GetAssetById(command.Id), cancellationToken);
            var attributesNeedValidateIds = asset.Attributes.Select(x => x.Id);

            try
            {
                var dependenciesOfAttribute = await GetDependencyOfAttributeAsync(attributesNeedValidateIds);
                var dependenciesOfAsset = await GetDependencyOfAssetAsync(new List<Guid>() { command.Id });

                var dependencies = dependenciesOfAttribute.Union(dependenciesOfAsset).Distinct();

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
            catch (EntityValidationException ex)
            {
                return new ValidateAssetResponse(false, ex.ErrorCode, ex.Payload);
            }

            return ValidateAssetResponse.Success;
        }

        public async Task<IEnumerable<AttributeDependency>> GetDependencyOfAttributeAsync(IEnumerable<Guid> attributeIds)
        {
            if (!attributeIds.Any())
                return Enumerable.Empty<AttributeDependency>();

            var assetAttributeDependencies = await _unitOfWork.AssetAttributes.GetAssetAttributeDependencyAsync(attributeIds.ToArray());
            var functionBlockDependencies = await _mediator.Send(new GetFunctionBlockExecutionAssetAttributeDependency(attributeIds.ToArray()));
            var alarmRuleDependencies = await _mediator.Send(new GetAlarmRuleAssetAttributeDependency(attributeIds.ToArray()));

            return assetAttributeDependencies.Select(x => AttributeDependency.Create(DependencyType.ASSET_ATTRIBUTE, x.AssetId, x.AssetAttributeName))
                                .Union(functionBlockDependencies.Select(x => AttributeDependency.Create(DependencyType.FUNCTION_BLOCK_EXECUTION, x.Id, x.Name)))
                                .Union(alarmRuleDependencies.Select(x => AttributeDependency.Create(DependencyType.ALARM_RULE, x.Id, x.Name)))
                                .Distinct();
        }

        public async Task<IEnumerable<AttributeDependency>> GetDependencyOfAssetAsync(IEnumerable<Guid> assetIds)
        {
            if (!assetIds.Any())
                return Enumerable.Empty<AttributeDependency>();

            var eventForwarding = await _mediator.Send(new GetEventForwardingUsingAsset(assetIds));

            return eventForwarding.Select(x => AttributeDependency.Create(DependencyType.EVENT_FORWADING, x.Id, x.Name))
                                .Distinct();
        }

        public async Task<IDictionary<string, object>> RetrieveAsync(RetrieveAsset request, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<ArchiveAssetDataDto>(request.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
            if (!data.Assets.Any())
            {
                return new Dictionary<string, object>();
            }
            var assets = data.Assets.OrderBy(x => x.CreatedUtc).Select(dto => ArchiveAssetDto.CreateEntity(dto, request.Upn)).ToList();
            _userContext.SetUpn(request.Upn);
            var updatedAssets = PreRetrieveProcessing(assets);
            await _unitOfWork.BeginTransactionAsync();
            try
            {

                await _unitOfWork.Assets.RetrieveAsync(assets);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
            var totalAsset = await _unitOfWork.Assets.GetTotalAssetAsync();
            var tasks = new List<Task>();
            foreach (var asset in assets)
            {
                tasks.Add(_dispatcher.SendAsync(AssetChangedEvent.CreateFrom(asset, totalAsset, _tenantContext)));
            }
            return updatedAssets.ToDictionary(x => x.Id.ToString(), x => (object)new
            {
                Name = x.Name.Base64Encode(),
                ParentId = x.ParentAssetId,
                ResourcePath = x.ResourcePath,
                TotalAsset = totalAsset
            });
        }

        private IEnumerable<Domain.Entity.Asset> PreRetrieveProcessing(IEnumerable<Domain.Entity.Asset> assets)
        {
            var updatedAssets = new List<Domain.Entity.Asset>();
            var duplicateNameGroups = assets.GroupBy(x => (x.ParentAssetId, x.Name));
            foreach (var group in duplicateNameGroups)
            {
                if (group.Skip(1).Any()) // if there are more than 1 name with the same parentId
                {
                    foreach (var asset in group)
                    {
                        var assetName = asset.Name;
                        // Support legacy name with old max length
                        if (assetName.Length > NameConstants.ASSET_NAME_MAX_LENGTH)
                        {
                            assetName = assetName.Substring(0, NameConstants.ASSET_NAME_MAX_LENGTH);
                        }
                        asset.Name = $"{assetName} ({asset.Id.ToString("D")})";

                        updatedAssets.Add(asset);
                    }
                }
            }
            return updatedAssets;
        }

        private async Task ValidateDeviceMappingAsync(IEnumerable<Domain.Entity.AssetAttributeTemplate> templateAttributes, IEnumerable<AttributeMapping> deviceMappings)
        {
            //Bug: https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_queries/edit/58249
            //Validate mappingDevice when create asset.
            if (templateAttributes.Any(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC) && deviceMappings.Any())
            {
                var deviceTemplates = await _readDeviceRepository.AsQueryable().Where(x => deviceMappings.Select(x => x.DeviceId).Any(a => a == x.Id)).Select(x => new { x.TemplateId, x.Id }).ToListAsync();
                var userMappings = deviceTemplates.Join(deviceMappings, m => m.Id, d => d.DeviceId, (m, d) => new { TemplateAttributeId = d.TemplateAttributeId, DeviceTemplateId = m.TemplateId });

                var templateAttributeDefine = templateAttributes
                                                .Where(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC)
                                                .Select(x => x.AssetAttributeDynamic)
                                                .Select(x => new { TemplateAttributeId = x.AssetAttributeTemplateId, DeviceTemplateId = x.DeviceTemplateId }).ToList();
                var mismatchDeviceTemplateIds = templateAttributeDefine.Except(userMappings);

                if (mismatchDeviceTemplateIds.Any())
                {
                    var failures = templateAttributes
                                        .Where(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC)
                                        .Select(x => x.AssetAttributeDynamic)
                                        .Where(x => mismatchDeviceTemplateIds.Any(a => a.DeviceTemplateId == x.DeviceTemplateId))
                                        .Select(x => new ValidationFailure($"Mapping_{x.AssetAttributeTemplateId}", ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID))
                                        .ToList();
                    throw EntityValidationExceptionHelper.GenerateException(failures, detailCode: MessageConstants.ASSET_MAPPING_DEVICE_INSTANCE_INVALID);
                }
            }
        }

        public async Task<ActivityResponse> ExportAttributesAsync(ExportAssetAttributes request, CancellationToken cancellationToken)
        {
            try
            {
                var ids = request.Ids.Select(x => new Guid(x));
                var existingEntityCount = await _unitOfWork.Assets.AsQueryable().CountAsync(x => ids.Contains(x.Id));
                if (existingEntityCount < ids.Count())
                {
                    throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
                }
                await _fileEventService.SendExportEventAsync(request.ActivityId, request.ObjectType, request.Ids);
                return new ActivityResponse(request.ActivityId);
            }
            catch
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_TEMPLATE, ActionType.Export, ActionStatus.Fail, payload: request);
                throw;
            }
        }

        public async Task<AssetAttributeParsedResponse> ParseAssetAttributesAsync(ParseAssetAttributes request, CancellationToken cancellationToken)
        {
            var result = new AssetAttributeParsedResponse();
            var client = _httpClientFactory.CreateClient(HttpClientNames.DEVICE_FUNCTION, _tenantContext);
            var body = new StringContent(JsonConvert.SerializeObject(new
            {
                AssetId = request.AssetId,
                FileName = request.FileName,
                ObjectType = request.ObjectType,
                Upn = _userContext.Upn,
                DateTimeFormat = _userContext.DateTimeFormat,
                DateTimeOffset = _userContext.Timezone?.Offset,
                UnsavedAttributes = request.UnsavedAttributes
            }), System.Text.Encoding.UTF8, "application/json");

            var responseMessage = await client.PostAsync($"fnc/dev/assets/attributes/parse", body);
            responseMessage.EnsureSuccessStatusCode();

            var stream = await responseMessage.Content.ReadAsByteArrayAsync();
            var assetAttributeParsed = stream.Deserialize<AssetAttributeParsedDto>();
            result.Attributes = assetAttributeParsed.Attributes.Select(AssetAttributeParsed.Create);
            result.Errors = assetAttributeParsed.Errors;
            return result;
        }

        private async Task HandleRedisCacheWhenEntityChangedAsync(Domain.Entity.Asset assetTracking)
        {
            var triggerKeyToRemove = CacheKey.AssetRuntimeTriggerPattern.GetCacheKey(_tenantContext.ProjectId);
            await _cache.DeleteHashByKeyAsync(triggerKeyToRemove, assetTracking.Id.ToString());
        }
    }

    public class AssetTracking
    {
        public Guid AssetId { get; set; }
        public string AssetName { get; set; }
        public Guid? ParentAssetId { get; set; }
        public ActionTypeEnum ActionType { get; set; }
        public string ResourcePath { get; set; }
        public bool DeleteTable { get; set; } = false;
        public bool DeleteMedia { get; set; } = false;
    }
}
