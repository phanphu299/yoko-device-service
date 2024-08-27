using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Service;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using AHI.Infrastructure.UserContext.Abstraction;
using Device.Application.Asset.Command;
using Device.Application.Constant;
using Device.Application.Exception;
using Device.Application.Models;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.Application.Template.Command;
using Device.Application.Template.Command.Model;
using Device.Application.TemplateDetail.Command;
using Device.Application.TemplateDetail.Command.Model;
using Device.ApplicationExtension.Extension;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using AHI.Infrastructure.Service.Tag.Extension;
using Device.Domain.Entity;

namespace Device.Application.Service
{
    public class DeviceTemplateService : BaseSearchService<Domain.Entity.DeviceTemplate, Guid, GetTemplateByCriteria, GetTemplateDto>, IDeviceTemplateService
    {
        private readonly IValidTemplateRepository _viewTemplateValidRepository;
        private readonly IReadTemplateKeyTypesRepository _readTemplateKeyTypesRepository;
        private readonly ITenantContext _tenantContext;
        private readonly IEntityLockService _entityLockService;
        private readonly IAuditLogService _auditLogService;
        private readonly IFileEventService _fileEventService;
        private readonly IUserContext _userContext;
        private readonly IDeviceUnitOfWork _deviceUnitOfWork;
        private readonly ILoggerAdapter<DeviceTemplateService> _logger;
        private readonly DeviceBackgroundService _deviceBackgroundService;
        private readonly ICache _cache;
        private readonly RuntimeDeviceTemplateAttributeHandler _runtimeAttributeHandler;
        private readonly IValidator<ArchiveTemplateDto> _templateVerifyValidator;
        private readonly IReadDeviceTemplateRepository _readDeviceTemplateRepository;
        private readonly IReadDeviceRepository _readDeviceRepository;
        private readonly IReadAssetAttributeSnapshotRepository _readAssetAttributeSnapshotRepository;
        private readonly IReadAssetAttributeRepository _readAssetAttributeRepository;
        private readonly IReadAssetAttributeTemplateRepository _readAssetAttributeTemplateRepository;
        private readonly ITagService _tagService;

        public DeviceTemplateService(
            IServiceProvider serviceProvider,
            IValidTemplateRepository viewTemplateValidRepository,
            IReadTemplateKeyTypesRepository readTemplateKeyTypesRepository,
            ITenantContext tenantContext,
            IEntityLockService entityLockService,
            IAuditLogService auditLogService,
            IFileEventService fileEventService,
            IUserContext userContext,
            IDeviceUnitOfWork deviceUnitOfWork,
            ILoggerAdapter<DeviceTemplateService> logger,
            RuntimeDeviceTemplateAttributeHandler runtimeAttributeHandler,
            ICache cache,
            IValidator<ArchiveTemplateDto> templateVerifyValidator,
            DeviceBackgroundService deviceBackgroundService,
            IReadDeviceTemplateRepository readDeviceTemplateRepository,
            IReadDeviceRepository readDeviceRepository,
            IReadAssetAttributeSnapshotRepository readAssetAttributeSnapshotRepository,
            IReadAssetAttributeRepository readAssetAttributeRepository,
            IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository,
            ITagService tagService
            )
            : base(GetTemplateDto.Create, serviceProvider)
        {
            _viewTemplateValidRepository = viewTemplateValidRepository;
            _readTemplateKeyTypesRepository = readTemplateKeyTypesRepository;
            _tenantContext = tenantContext;
            _entityLockService = entityLockService;
            _auditLogService = auditLogService;
            _fileEventService = fileEventService;
            _userContext = userContext;
            _deviceUnitOfWork = deviceUnitOfWork;
            _logger = logger;
            _runtimeAttributeHandler = runtimeAttributeHandler;
            _deviceBackgroundService = deviceBackgroundService;
            _templateVerifyValidator = templateVerifyValidator;
            _cache = cache;
            _readDeviceTemplateRepository = readDeviceTemplateRepository;
            _readDeviceRepository = readDeviceRepository;
            _readAssetAttributeSnapshotRepository = readAssetAttributeSnapshotRepository;
            _readAssetAttributeRepository = readAssetAttributeRepository;
            _readAssetAttributeTemplateRepository = readAssetAttributeTemplateRepository;
            _tagService = tagService;
        }

        protected override Type GetDbType()
        {
            return typeof(IDeviceTemplateRepository);
        }

        public async Task<IEnumerable<GetTemplateDetailsDto>> GetTemplateMetricsByTemplateIDAsync(GetTemplateMetricsByTemplateId request)
        {
            var template = await _readDeviceTemplateRepository.AsQueryable().AsNoTracking()
                                        .Include(x => x.Payloads).ThenInclude(x => x.Details)
                                        .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (template == null)
                return new List<GetTemplateDetailsDto>();

            if (template.Payloads == null || !template.Payloads.Any())
                return new List<GetTemplateDetailsDto>();

            var templateKeys = await _readTemplateKeyTypesRepository.AsQueryable().AsTracking().Where(x => x.Name == TemplateKeyTypeConstants.AGGREGATION || x.Name == TemplateKeyTypeConstants.METRIC).ToListAsync();
            var templateKeyIds = templateKeys.Select(x => x.Id).ToList();
            var details = template.Payloads.SelectMany(x => x.Details).Where(x => x != null)
                .Where(x => templateKeyIds.Any(k => k == x.KeyTypeId)).ToList();

            if (!request.IsIncludeDisabledMetric)
            {
                details = details.Where(x => x.Enabled).ToList();
            }

            return details.Select(GetTemplateDetailsDto.Create).ToList();
        }

        public async Task<AddTemplatesDto> AddEntityAsync(AddTemplates payload, CancellationToken token)
        {
            try
            {
                if (await IsDuplicationTemplateAsync(Guid.Empty, payload.Name))
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(Domain.Entity.DeviceTemplate.Name));

                var template = new Domain.Entity.DeviceTemplate() { Name = payload.Name, CreatedBy = _userContext.Upn };
                var metrics = payload.Payloads.SelectMany(x => x.Details).Select(x => (x.DetailId, x.Key, x.DataType));

                foreach (var p in payload.Payloads)
                {
                    var item = new Domain.Entity.TemplatePayload();
                    item.JsonPayload = p.JsonPayload;
                    item.Details = await Task.WhenAll(p.Details.Select(async entity =>
                    {
                        string expressionCompile = null;
                        bool validateResult = true;
                        if (!string.IsNullOrEmpty(entity.Expression))
                        {
                            var (runtimeValidateResult, runtimeExpressionCompile) = await _runtimeAttributeHandler.ValidateExpressionAsync(template.Id, entity.Expression, entity.DataType, metrics);
                            validateResult = runtimeValidateResult;
                            expressionCompile = runtimeExpressionCompile;
                        }
                        if (!validateResult)
                        {
                            _logger.LogError($"Expression validate exception - {entity.Expression}");
                            throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(Domain.Entity.TemplateDetail.Expression));
                        }
                        var detail = new Domain.Entity.TemplateDetail()
                        {
                            Key = entity.Key,
                            Name = entity.Name,
                            KeyTypeId = entity.KeyTypeId,
                            Expression = entity.Expression,
                            ExpressionCompile = expressionCompile,
                            Enabled = entity.Enabled,
                            DataType = entity.DataType,
                            DetailId = entity.DetailId
                        };
                        return detail;
                    }).ToList());

                    template.Payloads.Add(item);
                }

                foreach (var binding in payload.Bindings)
                {
                    var item = TemplateBinding.Command.AddTemplateBinding.Create(binding);
                    template.Bindings.Add(item);
                }

                var entityResult = await _deviceUnitOfWork.DeviceTemplates.AddEntityWithRelationAsync(template);

                var tagIds = Array.Empty<long>();
                payload.Upn = _userContext.Upn;
                payload.ApplicationId = Guid.Parse(_userContext.ApplicationId ?? ApplicationInformation.APPLICATION_ID);
                if (payload.Tags != null && payload.Tags.Any())
                {
                    tagIds = await _tagService.UpsertTagsAsync(payload);
                }

                if (tagIds.Any())
                {
                    var entitiesTags = tagIds.Distinct().Select(x => new EntityTagDb
                    {
                        EntityType = Privileges.DeviceTemplate.ENTITY_NAME,
                        EntityIdGuid = entityResult.Id,
                        TagId = x
                    }).ToArray();

                    await _deviceUnitOfWork.EntityTags.AddRangeWithSaveChangeAsync(entitiesTags);
                }

                await _deviceUnitOfWork.CommitAsync();

                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE_TEMPLATE, ActionType.Add, ActionStatus.Success, entityResult.Id, entityResult.Name, payload: payload);

                return await _tagService.FetchTagsAsync(AddTemplatesDto.Create(entityResult));
            }
            catch
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE_TEMPLATE, ActionType.Add, ActionStatus.Fail, payload: payload);
                throw;
            }
        }

        public async Task<UpdateTemplatesDto> UpdateEntityAsync(UpdateTemplates payload, CancellationToken token)
        {
            try
            {
                var templateDB = await _readDeviceTemplateRepository.AsQueryable()
                                    .Include(x => x.Bindings)
                                    .Include(x => x.Payloads).ThenInclude(x => x.Details).FirstOrDefaultAsync(x => x.Id == payload.Id);
                if (templateDB == null)
                    throw new EntityNotFoundException();
                if (await IsDuplicationTemplateAsync(payload.Id, payload.Name))
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(Domain.Entity.DeviceTemplate.Name));

                await ValidateIfEntityLockedAsync(payload.Id, token);

                var currentDetails = templateDB.Payloads.SelectMany(x => x.Details);
                var newDetailIds = payload.Payloads.SelectMany(x => x.Details).Select(x => x.Key);
                var deletedMetricKeys = currentDetails.Where(x => !newDetailIds.Contains(x.Key)).Select(x => x.Key);

                var usingMetricKeys = await _readAssetAttributeTemplateRepository.AsQueryable()
                                    .Include(x => x.AssetAttributeDynamic)
                                    .Where(x => x.AssetAttributeDynamic.DeviceTemplateId == payload.Id && deletedMetricKeys.Contains(x.AssetAttributeDynamic.MetricKey))
                                    .Select(x => x.AssetAttributeDynamic.MetricKey).ToListAsync();

                var usingDevices = await _readDeviceRepository.AsQueryable().Where(x => x.TemplateId == payload.Id).Select(x => x.Id).ToListAsync();

                usingMetricKeys.AddRange(await _readAssetAttributeRepository.AsQueryable()
                                            .Include(x => x.AssetAttributeDynamic)
                                            .Where(x => usingDevices.Contains(x.AssetAttributeDynamic.DeviceId) && deletedMetricKeys.Contains(x.AssetAttributeDynamic.MetricKey))
                                            .Select(x => x.AssetAttributeDynamic.MetricKey).ToListAsync());

                var deletedPayloads = templateDB.Payloads.Where(x => !payload.Payloads.Any(y => y.Id == x.Id)).SelectMany(x => x.Details).ToList();

                if (deletedPayloads.Select(x => x.Key).Any(x => usingMetricKeys.Contains(x)))
                    throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_TEMPLATE_PAYLOAD_USING);

                var metricsUsingInExpression = payload.Payloads.SelectMany(x => x.Details).Where(x => !string.IsNullOrWhiteSpace(x.Expression)).SelectMany(x => GetDetailIdsFromExpression(x.Expression)).ToList();
                var deletedMetricIds = deletedPayloads.Select(x => x.DetailId)
                                .Union(currentDetails.Where(x => !payload.Payloads.SelectMany(x => x.Details.Select(y => y.DetailId)).Contains(x.DetailId)).Select(x => x.DetailId));
                if (deletedMetricIds.Any(x => metricsUsingInExpression.Contains(x)))
                    throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_TEMPLATE_PAYLOAD_METRIC_USING_EXPRESSION);

                if (usingMetricKeys.Any())
                    throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_TEMPLATE_METRIC_USING);

                var entity = new Domain.Entity.DeviceTemplate() { Name = payload.Name, Id = payload.Id, TotalMetric = payload.TotalMetric, Deleted = payload.Deleted };
                var metrics = payload.Payloads.SelectMany(x => x.Details).Select(x => (x.DetailId, x.Key, x.DataType)).ToList();
                foreach (var p in payload.Payloads)
                {
                    var item = new Domain.Entity.TemplatePayload();
                    item.JsonPayload = p.JsonPayload;
                    item.TemplateId = p.TemplateId;
                    item.Id = p.Id;
                    item.Details = await GetTemplateDetailAsync(payload.Id, p.Details, metrics);
                    entity.Payloads.Add(item);
                }

                foreach (var binding in payload.Bindings)
                {
                    var item = TemplateBinding.Command.UpdateTemplateBinding.Create(binding);
                    entity.Bindings.Add(item);
                }

                var updateBindingIds = entity.Bindings.Select(x => x.Id).ToList();
                var deletedBindingKeys = templateDB.Bindings.Where(x => !updateBindingIds.Contains(x.Id)).Select(x => x.Key).ToList();

                var relatedDeviceIds = await _readDeviceRepository.AsQueryable().AsNoTracking().Where(x => x.TemplateId == templateDB.Id).Select(x => x.Id).ToListAsync();
                var isBindingInAssets = await _readAssetAttributeRepository.AsQueryable().Where(x => x.AssetAttributeCommand != null)
                                        .AnyAsync(x => deletedBindingKeys.Any(y => y == x.AssetAttributeCommand.MetricKey) && relatedDeviceIds.Any(y => y == x.AssetAttributeCommand.DeviceId));
                var isBindingInAssetTemplate = await _readAssetAttributeTemplateRepository.AsQueryable()
                                        .AsNoTracking().Where(x => x.AssetAttributeCommand != null)
                                        .AnyAsync(x => deletedBindingKeys.Any(y => y == x.AssetAttributeCommand.MetricKey) && x.AssetAttributeCommand.DeviceTemplateId == templateDB.Id);

                if (isBindingInAssets || isBindingInAssetTemplate)
                    throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_TEMPLATE_BINDING_USING);

                var entityResult = await _deviceUnitOfWork.DeviceTemplates.UpdateEntityWithRelationAsync(payload.Id, entity);
                await _entityLockService.AcceptEntityUnlockRequestAsync(new EntityLock.Command.AcceptEntityUnlockRequestCommand()
                {
                    TargetId = payload.Id
                }, CancellationToken.None);

                var tagIds = Array.Empty<long>();
                payload.Upn = _userContext.Upn;
                payload.ApplicationId = Guid.Parse(_userContext.ApplicationId ?? ApplicationInformation.APPLICATION_ID);

                if (payload.Tags != null && payload.Tags.Any())
                {
                    tagIds = await _tagService.UpsertTagsAsync(payload);
                }

                await _deviceUnitOfWork.EntityTags.RemoveByEntityIdAsync(Privileges.DeviceTemplate.ENTITY_NAME, entityResult.Id);

                if (tagIds.Any())
                {
                    var entitiesTags = tagIds.Distinct().Select(x => new EntityTagDb
                    {
                        EntityType = Privileges.DeviceTemplate.ENTITY_NAME,
                        EntityIdGuid = entityResult.Id,
                        TagId = x
                    }).ToArray();
                    await _deviceUnitOfWork.EntityTags.AddRangeWithSaveChangeAsync(entitiesTags);
                }

                await _deviceUnitOfWork.CommitAsync();

                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE_TEMPLATE, ActionType.Update, ActionStatus.Success, entityResult.Id, entityResult.Name, payload);

                // cleanup the cache
                var deviceIds = await _readDeviceRepository.AsQueryable().AsNoTracking().Where(x => x.TemplateId == payload.Id).Select(x => x.Id).ToListAsync();
                await _readAssetAttributeSnapshotRepository.Snapshots.AsNoTracking().Where(x => deviceIds.Contains(x.DeviceId)).Select(x => x.AssetId).ToListAsync();

                var deviceMetricKeyDeviceHashField = CacheKey.PROCESSING_DEVICE_HASH_FIELD.GetCacheKey(string.Empty, "metric_device_id_key");
                var deviceMetricKeyDeviceIdHashKey = CacheKey.PROCESSING_DEVICE_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);

                await _cache.DeleteHashByKeyAsync(deviceMetricKeyDeviceIdHashKey, deviceMetricKeyDeviceHashField);
                await _deviceBackgroundService.QueueAsync(_tenantContext, new CleanAssetCache());

                return await _tagService.FetchTagsAsync(UpdateTemplatesDto.Create(entityResult));
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error occurs in DeviceTemplateService - UpdateEntityAsync");
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE_TEMPLATE, ActionType.Update, ActionStatus.Fail, payload.Id, payload.Name, payload: payload);
                throw;
            }
        }

        private IEnumerable<Guid> GetDetailIdsFromExpression(string expression)
        {
            var result = new List<Guid>();
            if (string.IsNullOrEmpty(expression))
                return result;
            Match m = Regex.Match(expression, RegexConstants.PATTERN_EXPRESSION_KEY, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(10));
            //get metric name in expression
            while (m.Success)
            {
                string idProperty = m.Value.Replace("${", "").Replace("}$", "").Trim();
                if (Guid.TryParse(idProperty, out Guid detailId))
                    result.Add(detailId);
                m = m.NextMatch();
            }
            return result;
        }

        private async Task<ICollection<Domain.Entity.TemplateDetail>> GetTemplateDetailAsync(Guid templateId, ICollection<UpdateTemplateDetails> details, IList<(Guid, string, string)> metrics)
        {
            var templateDetails = new List<Domain.Entity.TemplateDetail>();
            foreach (var item in details)
            {
                string expressionCompile = null;
                bool validateResult = true;
                if (!string.IsNullOrEmpty(item.Expression))
                {
                    var (runtimeValidateResult, runtimeExpressionCompile) = await _runtimeAttributeHandler.ValidateExpressionAsync(templateId, item.Expression, item.DataType, metrics);
                    validateResult = runtimeValidateResult;
                    expressionCompile = runtimeExpressionCompile;
                }
                if (!validateResult)
                {
                    _logger.LogError($"Expression validate exception - {item.Expression}");
                    throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(UpdateTemplateDetails.Expression));
                }
                var detail = new Domain.Entity.TemplateDetail()
                {
                    Id = item.Id,
                    Key = item.Key,
                    Name = item.Name,
                    KeyTypeId = item.KeyTypeId,
                    Expression = item.Expression,
                    ExpressionCompile = expressionCompile,
                    Enabled = item.Enabled,
                    DataType = item.DataType,
                    DetailId = item.DetailId
                };
                templateDetails.Add(detail);
            }
            return templateDetails;
        }

        public async Task<GetTemplateDto> FindEntityByIdAsync(GetTemplateByID payload, CancellationToken token)
        {
            Domain.Entity.DeviceTemplate entity = await _readDeviceTemplateRepository.FindEntityWithRelationAsync(payload.Id);
            if (entity == null)
                throw new EntityNotFoundException();
            var lockEntity = await _entityLockService.GetEntityLockedAsync(payload.Id);
            var response = GetTemplateDto.Create(entity);
            response.LockedByUpn = lockEntity == null ? null : lockEntity.CurrentUserUpn;

            return await _tagService.FetchTagsAsync(response);
        }

        public async Task<IEnumerable<GetValidTemplateDto>> FindAllEntityWithDefaultAsync(GetTemplateByDefault payload, CancellationToken token)
        {
            var queryable = _viewTemplateValidRepository.AsQueryable();
            queryable = queryable.OrderBy(x => x.Name);

            var result = await queryable.ToListAsync();
            return result.Select(GetValidTemplateDto.Create);
        }

        public override async Task<BaseSearchResponse<GetTemplateDto>> SearchAsync(GetTemplateByCriteria request)
        {
            request.MappingSearchTags();
            BaseSearchResponse<GetTemplateDto> response = await base.SearchAsync(request);

            if (response != null)
                return await _tagService.FetchTagsAsync(response);
            else
                return response;
        }

        public override async Task<GetTemplateDto> FetchAsync(Guid id)
        {
            GetTemplateDto response = await base.FetchAsync(id);

            if (response != null)
                return await _tagService.FetchTagsAsync(response);
            else
                return response;
        }

        public async Task<BaseResponse> DeleteEntityAsync(DeleteTemplates payload, CancellationToken token)
        {
            var templates = await _readDeviceTemplateRepository.AsQueryable().Where(x => payload.TemplateIds.Contains(x.Id)).ToListAsync();
            await _deviceUnitOfWork.BeginTransactionAsync();

            try
            {
                await ValidateExistTemplateAsync(payload.TemplateIds);
                await ValidateIfEntitiesLockedAsync(payload.TemplateIds, token);
                foreach (var id in payload.TemplateIds)
                {
                    await RemoveDeviceTemplateAsync(id);
                }
                await _deviceUnitOfWork.CommitAsync();
            }
            catch
            {
                await _deviceUnitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE_TEMPLATE, ActionType.Delete, ActionStatus.Fail, payload.TemplateIds, templates.Select(x => x.Name), payload: payload);
                throw;
            }
            await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE_TEMPLATE, ActionType.Delete, ActionStatus.Success, payload.TemplateIds, templates.Select(x => x.Name), payload: payload);
            // cleanup the cache
            await _deviceBackgroundService.QueueAsync(_tenantContext, new CleanAssetCache());

            return BaseResponse.Success;
        }

        private async Task ValidateExistTemplateAsync(IEnumerable<Guid> ids)
        {
            var requestIdCount = ids.Count();
            var existingEntityCount = await _readDeviceTemplateRepository.AsQueryable().CountAsync(x => ids.Contains(x.Id));
            if (existingEntityCount < requestIdCount)
            {
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
            }
        }

        private async Task RemoveDeviceTemplateAsync(Guid id)
        {
            var entity = await _readDeviceTemplateRepository.AsQueryable()
            .Include(x => x.AssetAttributeDynamicTemplates)
            .Include(x => x.Bindings)
            .Include(x => x.Devices).FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
            if (entity.Devices.Any())
                throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_TEMPLATE_USING);

            if (entity.AssetAttributeDynamicTemplates.Any())
                throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_TEMPLATE_USING);

            if (entity.AssetAttributeCommandTemplates.Any())
                throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_TEMPLATE_USING);

            var deletedBindingKeys = entity.Bindings.Select(x => x.Key);
            var relatedDeviceIds = await _readDeviceRepository.AsQueryable().AsNoTracking().Where(x => x.TemplateId == entity.Id).Select(x => x.Id).ToListAsync();
            var isBindingInAssets = await _readAssetAttributeRepository.AsQueryable().Where(x => x.AssetAttributeCommand != null)
                                    .AnyAsync(x => deletedBindingKeys.Any(y => y == x.AssetAttributeCommand.MetricKey) && relatedDeviceIds.Any(y => y == x.AssetAttributeCommand.DeviceId));
            var isBindingInAssetTemplate = await _readAssetAttributeTemplateRepository.AsQueryable()
                                    .AsNoTracking().Where(x => x.AssetAttributeCommand != null)
                                    .AnyAsync(x => deletedBindingKeys.Any(y => y == x.AssetAttributeCommand.MetricKey) && x.AssetAttributeCommand.DeviceTemplateId == entity.Id);

            if (isBindingInAssets || isBindingInAssetTemplate)
                throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_TEMPLATE_BINDING_USING);

            await _deviceUnitOfWork.DeviceTemplates.RemoveAsync(id);
        }

        public async Task<ActivityResponse> ExportAsync(ExportDeviceTemplate request, CancellationToken cancellationToken)
        {
            try
            {
                var ids = request.Ids.Select(x => Guid.Parse(x));
                var existingEntityCount = await _readDeviceTemplateRepository.AsQueryable().Where(x => ids.Contains(x.Id)).ToListAsync();
                if (!existingEntityCount.Any())
                {
                    throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
                }
                await _fileEventService.SendExportEventAsync(request.ActivityId, request.ObjectType, request.Ids);
                return new ActivityResponse(request.ActivityId);
            }
            catch (System.Exception)
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE_TEMPLATE, ActionType.Export, ActionStatus.Fail, payload: request);
                throw;
            }
        }

        public async Task<bool> CheckExistMetricByTemplateIdAsync(string metricKey, Guid? templateId)
        {
            if (string.IsNullOrWhiteSpace(metricKey) || templateId == null)
                return false;

            var metrics = await GetTemplateMetricsByTemplateIDAsync(new GetTemplateMetricsByTemplateId(templateId.Value, true));

            return metrics.Any(x => x.Key == metricKey);
        }

        public async Task<bool> CheckExistBindingsByTemplateIdAsync(string bindingKey, Guid? templateId)
        {
            if (string.IsNullOrWhiteSpace(bindingKey) || templateId == null)
                return false;

            var deviceTemplate = await _readDeviceTemplateRepository.FindEntityWithRelationAsync(templateId.Value);
            var bindingKeys = deviceTemplate?.Bindings.Select(x => x.Key) ?? Enumerable.Empty<string>();

            return bindingKeys.Any(x => x == bindingKey);
        }

        private async Task<bool> IsDuplicationTemplateAsync(Guid id, string name)
        {
            return await _readDeviceTemplateRepository.AsQueryable().AsNoTracking()
                                                .Where(x => x.Name.ToLower() == name.ToLower())
                                                .Where(x => x.Id != id)
                                                .AnyAsync();
        }

        public async Task<bool> ValidationTemplateDetailsAsync(Guid id, IEnumerable<string> keys)
        {
            var result = true;
            if (keys == null || !keys.Any())
                return false;
            if (id == Guid.Empty)
                return true;
            var template = await _readDeviceTemplateRepository.FindEntityWithRelationAsync(id);
            if (template == null)
                return false;

            foreach (var key in keys)
            {
                var isAttributesUsing = await _readDeviceTemplateRepository.ValidationAttributeUsingMetricsAsync(template, key);
                if (isAttributesUsing)
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        public async Task<IEnumerable<ArchiveTemplateDto>> ArchiveAsync(ArchiveTemplate command, CancellationToken token)
        {
            return await _readDeviceTemplateRepository.AsQueryable()
                                                    .AsNoTracking()
                                                    .Include(x => x.Devices)
                                                    .Include(x => x.Bindings)
                                                    .Include(x => x.Payloads).ThenInclude(x => x.Details).ThenInclude(x => x.TemplateKeyType)
                                                    .Include(x => x.Payloads).ThenInclude(x => x.Details)
                                                    .Include(x => x.Bindings)
                                                    .Include(x => x.EntityTags)
                                                    .Where(a => !a.Deleted && a.UpdatedUtc <= command.ArchiveTime && (!a.EntityTags.Any() || a.EntityTags.Any(e => e.EntityType == Privileges.DeviceTemplate.ENTITY_NAME)))
                                                    .Select(template => ArchiveTemplateDto.Create(template))
                                                    .ToListAsync();
        }

        public async Task<BaseResponse> RetrieveAsync(RetrieveTemplate command, CancellationToken token)
        {
            var inputData = JsonConvert.DeserializeObject<IEnumerable<ArchiveTemplateDto>>(command.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
            if (!inputData.Any())
            {
                return BaseResponse.Success;
            }
            var entities = inputData.OrderBy(x => x.UpdatedUtc).Select(templateDto => ArchiveTemplateDto.Create(templateDto, command.Upn));
            _userContext.SetUpn(command.Upn);
            await _deviceUnitOfWork.BeginTransactionAsync();
            try
            {
                await _deviceUnitOfWork.DeviceTemplates.RetrieveAsync(entities);
                await _deviceUnitOfWork.CommitAsync();
            }
            catch
            {
                await _deviceUnitOfWork.RollbackAsync();
                throw;
            }
            return BaseResponse.Success;
        }

        public async Task<BaseResponse> VerifyArchiveAsync(VerifyTemplate command, CancellationToken token)
        {
            var inputData = JsonConvert.DeserializeObject<IEnumerable<ArchiveTemplateDto>>(command.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
            foreach (var template in inputData)
            {
                var validation = await _templateVerifyValidator.ValidateAsync(template);
                if (!validation.IsValid)
                {
                    throw EntityValidationExceptionHelper.GenerateException(nameof(command.Data), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
                }
            }
            return BaseResponse.Success;
        }

        private Task ValidateIfEntityLockedAsync(Guid entityId, CancellationToken cancellationToken)
        {
            var lstEntitiesId = new List<Guid> { entityId };
            return ValidateIfEntitiesLockedAsync(lstEntitiesId, cancellationToken);
        }

        private async Task ValidateIfEntitiesLockedAsync(IEnumerable<Guid> entityIds, CancellationToken cancellationToken)
        {
            var isLocked = await _entityLockService.ValidateEntitiesLockedByOtherAsync(new EntityLock.Command.ValidateLockEntitiesCommand(entityIds)
            {
                HolderUpn = _userContext.Upn,
            }, cancellationToken);
            if (isLocked)
            {
                throw LockException.CreateAlreadyLockException();
            }
        }

        public Task<BaseResponse> CheckExistDeviceTemplatesAsync(CheckExistTemplate deviceTemplates, CancellationToken cancellationToken)
        {
            return ValidateExistDeviceTemplatesAsync(deviceTemplates);
        }

        private async Task<BaseResponse> ValidateExistDeviceTemplatesAsync(CheckExistTemplate command)
        {
            var requestIds = new HashSet<Guid>(command.Ids.Distinct());
            var templates = new HashSet<Guid>(await _readDeviceTemplateRepository.AsQueryable().AsNoTracking().Where(x => requestIds.Contains(x.Id)).Select(x => x.Id).ToListAsync());
            if (!requestIds.SetEquals(templates))
                throw new EntityNotFoundException();
            return BaseResponse.Success;
        }

        public async Task<bool> CheckMetricUsingAsync(CheckMetricUsing command, CancellationToken token)
        {
            var templateDB = await _readDeviceTemplateRepository.AsQueryable()
                                    .Include(x => x.Bindings)
                                    .Include(x => x.Payloads).ThenInclude(x => x.Details).FirstOrDefaultAsync(x => x.Id == command.Id);
            if (templateDB == null)
                throw new EntityNotFoundException();
            var detailIds = templateDB.Payloads.SelectMany(x => x.Details).SelectMany(x => GetDetailIdsFromExpression(x.Expression));
            return detailIds.Contains(command.DetailId);
        }

        public async Task<bool> CheckBindingUsingAsync(CheckBindingUsing command, CancellationToken token)
        {
            var template = await _readDeviceTemplateRepository.AsQueryable()
                                    .Include(x => x.Bindings)
                                    .FirstOrDefaultAsync(x => x.Id == command.Id);
            if (template == null)
                throw new EntityNotFoundException(MessageConstants.DEVICE_TEMPLATE_NOT_FOUND);
            var binding = template.Bindings.FirstOrDefault(x => x.Id == command.BindingId);
            if (binding == null)
                throw new EntityNotFoundException();
            var relatedDeviceIds = await _readDeviceRepository.AsQueryable().AsNoTracking().Where(x => x.TemplateId == template.Id).Select(x => x.Id).ToListAsync();
            var isBindingInAssets = await _readAssetAttributeRepository.AsQueryable().AsNoTracking().Where(x => x.AssetAttributeCommand != null)
                                        .AnyAsync(x => binding.Key == x.AssetAttributeCommand.MetricKey && relatedDeviceIds.Any(y => y == x.AssetAttributeCommand.DeviceId));
            var isBindingInAssetTemplate = await _readAssetAttributeTemplateRepository.AsQueryable()
                                    .AsNoTracking().Where(x => x.AssetAttributeCommand != null)
                                    .AnyAsync(x => binding.Key == x.AssetAttributeCommand.MetricKey && x.AssetAttributeCommand.DeviceTemplateId == template.Id);

            return isBindingInAssets || isBindingInAssetTemplate;
        }
    }
}
