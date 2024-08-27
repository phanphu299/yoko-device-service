using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AHI.Device.Application.Constant;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.Service;
using AHI.Infrastructure.Service.Tag.Extension;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using AHI.Infrastructure.UserContext.Abstraction;
using Device.Application.Asset.Command;
using Device.Application.Asset.Command.Model;
using Device.Application.AssetAttributeTemplate.Command.Model;
using Device.Application.AssetTemplate.Command;
using Device.Application.AssetTemplate.Command.Model;
using Device.Application.Constant;
using Device.Application.Events;
using Device.Application.Exception;
using Device.Application.Model;
using Device.Application.Models;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.Service.Tag.Extension;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using Device.Domain.Entity;
using AHI.Infrastructure.Service.Tag.Enum;
using Device.ApplicationExtension.Extension;

namespace Device.Application.Service
{
    public class AssetTemplateService : BaseSearchService<Domain.Entity.AssetTemplate, Guid, GetAssetTemplateByCriteria, GetAssetTemplateDto>, IAssetTemplateService
    {
        private readonly IAssetAttributeHandler _assetAttributeHandler;
        private readonly IAssetAttributeTemplateService _assetAttributeTemplateService;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;
        private readonly IAssetTemplateUnitOfWork _unitOfWork;
        private readonly IAssetService _assetService;
        private readonly IFileEventService _fileEventService;
        private readonly IUserContext _userContext;
        private readonly ILoggerAdapter<AssetTemplateService> _logger;
        private readonly IEntityLockService _entityLockService;
        private readonly ITenantContext _tenantContext;
        private readonly DeviceBackgroundService _deviceBackgroundService;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IValidator<ArchiveAssetTemplateDto> _templateVerifyValidator;
        private readonly ICache _cache;
        private readonly IDomainEventDispatcher _dispatcher;
        private readonly IReadAssetTemplateRepository _readAssetTemplateRepository;
        private readonly IReadDeviceRepository _readDeviceRepository;
        private readonly IReadAssetRepository _readAssetRepository;
        private readonly IReadDeviceTemplateRepository _readDeviceTemplateRepository;
        private readonly IReadAssetAttributeTemplateRepository _readAssetAttributeTemplateRepository;
        private readonly ITagService _tagService;

        public AssetTemplateService(
            IAssetAttributeHandler assetAttributeHandler,
            IServiceProvider serviceProvider,
            IAuditLogService auditLogService,
            INotificationService notificationService,
            IAssetTemplateUnitOfWork unitOfWork,
            IFileEventService fileEventService,
            IAssetAttributeTemplateService assetAttributeTemplateService,
            IUserContext userContext,
            ILoggerAdapter<AssetTemplateService> logger,
            IEntityLockService entityLockService,
            ITenantContext tenantContext,
            DeviceBackgroundService deviceBackgroundService,
            IAssetService assetService,
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            ICache cache,
            IValidator<ArchiveAssetTemplateDto> templateVerifyValidator,
            IDomainEventDispatcher dispatcher,
            IReadAssetTemplateRepository readAssetTemplateRepository,
            IReadDeviceRepository readDeviceRepository,
            IReadAssetRepository readAssetRepository,
            IReadDeviceTemplateRepository readDeviceTemplateRepository,
            IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository,
            ITagService tagService)
             : base(GetAssetTemplateDto.Create, serviceProvider)
        {
            _assetAttributeHandler = assetAttributeHandler;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _fileEventService = fileEventService;
            _userContext = userContext;
            _assetAttributeTemplateService = assetAttributeTemplateService;
            _logger = logger;
            _entityLockService = entityLockService;
            _tenantContext = tenantContext;
            _deviceBackgroundService = deviceBackgroundService;
            _assetService = assetService;
            _clientFactory = clientFactory;
            _templateVerifyValidator = templateVerifyValidator;
            _cache = cache;
            _dispatcher = dispatcher;
            _readAssetTemplateRepository = readAssetTemplateRepository;
            _readDeviceRepository = readDeviceRepository;
            _readAssetRepository = readAssetRepository;
            _readDeviceTemplateRepository = readDeviceTemplateRepository;
            _readAssetAttributeTemplateRepository = readAssetAttributeTemplateRepository;
            _tagService = tagService;
        }
        protected override Type GetDbType()
        {
            return typeof(IAssetTemplateRepository);
        }

        public async Task<GetAssetTemplateDto> FindTemplateByIdAsync(GetAssetTemplateById command, CancellationToken cancellationToken)
        {
            var assetTemplate = await _unitOfWork.Templates.GetAssetTemplateAsync(command.Id);
            if (assetTemplate == null)
                throw new EntityNotFoundException();
            var lockEntity = await _entityLockService.GetEntityLockedAsync(command.Id);


            var templateDetails = _unitOfWork.TemplateDetails.AsQueryable()
                                                                .Include(x => x.TemplateKeyType)
                                                                .Include(x => x.Payload)
                                                                .Where(x => x.TemplateKeyType.Name == TemplateKeyTypeConstants.METRIC
                                                                            || x.TemplateKeyType.Name == TemplateKeyTypeConstants.AGGREGATION);
            foreach (var attribute in assetTemplate.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC))
            {
                attribute.Payload.MetricName = templateDetails.FirstOrDefault(d => d.Key == attribute.Payload.MetricKey && d.Payload.TemplateId == attribute.Payload.DeviceTemplateId)?.Name;
            }

            assetTemplate.LockedByUpn = lockEntity == null ? null : lockEntity.CurrentUserUpn;

            return await _tagService.FetchTagsAsync(assetTemplate);

        }

        public async Task<AddAssetTemplateDto> AddAssetTemplateAsync(AddAssetTemplate command, CancellationToken cancellationToken)
        {
            var entity = AddAssetTemplate.Create(command);
            entity.CreatedBy = _userContext.Upn;
            Task<long[]> upsertTagsTask = null;
            command.Upn = _userContext.Upn;
            command.ApplicationId = Guid.Parse(!string.IsNullOrEmpty(_userContext.ApplicationId) ? _userContext.ApplicationId : ApplicationInformation.APPLICATION_ID);
            if (command.Tags != null && command.Tags.Any())
            {
                upsertTagsTask = _tagService.UpsertTagsAsync(command);
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                if (await IsDuplicationAssetTemplateAsync(command.Name))
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(Domain.Entity.AssetTemplate.Name));

                var entityResult = await _unitOfWork.Templates.AddEntityAsync(entity);
                await ProcessAddAsync(GetAssetTemplateDto.Create(entityResult), command, cancellationToken);

                var tagIds = Array.Empty<long>();
                if (upsertTagsTask != null)
                    tagIds = await upsertTagsTask;

                if (tagIds.Any())
                {
                    var entitiesTags = tagIds.Distinct().Select(x => new EntityTagDb
                    {
                        EntityType = EntityTypeConstants.ASSET_TEMPLATE,
                        EntityIdGuid = entityResult.Id,
                        TagId = x
                    }).ToArray();

                    await _unitOfWork.EntityTags.AddRangeWithSaveChangeAsync(entitiesTags);
                }

                await _unitOfWork.CommitAsync();
            }
            catch (System.Exception exc)
            {
                _logger.LogError(exc, exc.Message);
                await _unitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_TEMPLATE, ActionType.Add, ActionStatus.Fail, payload: command);
                throw;
            }
            await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_TEMPLATE, ActionType.Add, ActionStatus.Success, entity.Id, entity.Name, command);
            return await _tagService.FetchTagsAsync(AddAssetTemplateDto.Create(entity));
        }

        public async Task<AddAssetTemplateDto> CreateAssetTemplateFromAssetAsync(CreateAssetTemplateFromAsset command, CancellationToken cancellationToken)
        {
            var assetDto = await _assetService.FindAssetByIdAsync(new Asset.Command.GetAssetById(command.Id), cancellationToken);
            var addAssetTemplate = new AddAssetTemplate()
            {
                Name = $"From Asset {assetDto.Name} - {Guid.NewGuid().ToString("D")}"
            };

            if (addAssetTemplate.Name.Length > NameConstants.NAME_MAX_LENGTH)
                throw new EntityInvalidException(detailCode: MessageConstants.ASSET_CREATE_ASSET_TEMPLATE_NAME_MAX_LENGTH);

            var templateAttributes = new List<AssetTemplateAttribute>();
            var idMappings = new Dictionary<Guid, Guid>();
            var aliasIds = assetDto.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS).Select(x => x.Id).ToList();

            // for static, dynamic, integration
            await CreateNonRuntimeTemplateAttributesAsync(assetDto, templateAttributes, idMappings);
            CreateRuntimeTemplateAttributes(assetDto, templateAttributes, idMappings, aliasIds);
            ProcessRuntimeTemplateAttributeExpressions(assetDto, templateAttributes, idMappings);
            ProcessAliasTemplateAttributes(templateAttributes);

            addAssetTemplate.Attributes = templateAttributes;
            return await AddAssetTemplateAsync(addAssetTemplate, cancellationToken);
        }

        private async Task CreateNonRuntimeTemplateAttributesAsync(GetAssetDto assetDto, ICollection<AssetTemplateAttribute> templateAttributes, IDictionary<Guid, Guid> idMappings)
        {
            foreach (var attribute in assetDto.Attributes.Where(x => x.AttributeType != AttributeTypeConstants.TYPE_RUNTIME))
            {
                var payload = attribute.Payload;

                //Remove markupName because create asset template from asset
                if (payload != null && payload.ContainsKey(AssetTemplateConstants.MARKUP_NAME))
                {
                    payload.Remove(AssetTemplateConstants.MARKUP_NAME);
                }

                var newAttributeValue = await DecorateNonRuntimeAttributeAsync(attribute, payload);

                // create the asset template attributes
                var templateAttribute = new AssetTemplateAttribute()
                {
                    Name = attribute.Name,
                    Value = newAttributeValue,
                    DataType = attribute.DataType,
                    UomId = attribute.UomId,
                    AttributeType = attribute.AttributeType,
                    Payload = payload,
                    DecimalPlace = attribute.DataType == DataTypeConstants.TYPE_DOUBLE ? attribute.DecimalPlace : null,
                    ThousandSeparator = attribute.ThousandSeparator
                };
                templateAttributes.Add(templateAttribute);
                idMappings[attribute.Id] = templateAttribute.Id;
            }
        }

        private void CreateRuntimeTemplateAttributes(GetAssetDto assetDto, ICollection<AssetTemplateAttribute> templateAttributes, IDictionary<Guid, Guid> idMappings, IEnumerable<Guid> aliasIds)
        {
            var deletedAttributeIds = new List<Guid>();

            foreach (var attribute in assetDto.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_RUNTIME))
            {
                if (attribute.Payload.EnabledExpression)
                {
                    var expression = attribute.Payload[AssetTemplateConstants.EXPRESSION].ToString();
                    // the expression should be: ${guid-id}
                    // skip the runtime attribute if it contains the alias attribute in the expression.
                    if (aliasIds.Any(att => expression.Contains($"${{{att}}}")))
                    {
                        deletedAttributeIds.Add(attribute.Id);
                        continue;
                    }
                    if (deletedAttributeIds.Any(attributeId => expression.Contains($"${{{attributeId}}}")))
                    {
                        // if the expression contains the delete attributes -> need to ignore this attribut as well.
                        // https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/5365
                        deletedAttributeIds.Add(attribute.Id);
                        continue;
                    }
                }

                // create the asset template attributes
                var templateAttribute = new AssetTemplateAttribute()
                {
                    Name = attribute.Name,
                    DataType = attribute.DataType,
                    UomId = attribute.UomId,
                    AttributeType = attribute.AttributeType,
                    Payload = attribute.Payload,
                    DecimalPlace = attribute.DataType == DataTypeConstants.TYPE_DOUBLE ? attribute.DecimalPlace : null,
                    ThousandSeparator = attribute.ThousandSeparator,
                };

                idMappings[attribute.Id] = templateAttribute.Id;
                templateAttributes.Add(templateAttribute);
            }
        }

        private void ProcessRuntimeTemplateAttributeExpressions(GetAssetDto assetDto, ICollection<AssetTemplateAttribute> templateAttributes, IDictionary<Guid, Guid> idMappings)
        {
            var runtimePayloads = templateAttributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_RUNTIME).Select(x => x.Payload);
            foreach (var runtimeAttributePayload in runtimePayloads.Where(payload => payload.EnabledExpression && payload.ContainsKey(AssetTemplateConstants.EXPRESSION)))
            {
                var expression = runtimeAttributePayload[AssetTemplateConstants.EXPRESSION]?.ToString() ?? string.Empty;
                // replace the expression
                foreach (var mapping in idMappings)
                {
                    expression = expression.Replace(mapping.Key.ToString(), mapping.Value.ToString());
                }

                runtimeAttributePayload[AssetTemplateConstants.EXPRESSION] = expression;

                var triggerAttributeId = Guid.Empty;
                if (runtimeAttributePayload.ContainsKey(AssetTemplateConstants.TRIGGER_ATTRIBUTE_ID))
                {
                    Guid.TryParse(runtimeAttributePayload[AssetTemplateConstants.TRIGGER_ATTRIBUTE_ID]?.ToString(), out triggerAttributeId);
                }

                var triggerAttribute = assetDto.Attributes.FirstOrDefault(x => (x.AttributeType == AttributeTypeConstants.TYPE_RUNTIME || x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC) && x.Id == triggerAttributeId);

                if (triggerAttribute == null)
                {
                    runtimeAttributePayload[AssetTemplateConstants.TRIGGER_ATTRIBUTE_ID] = null;
                }
                else
                {
                    runtimeAttributePayload[AssetTemplateConstants.TRIGGER_ATTRIBUTE_ID] = idMappings[triggerAttributeId];
                }
            }
        }

        private void ProcessAliasTemplateAttributes(ICollection<AssetTemplateAttribute> templateAttributes)
        {
            foreach (var aliasAttribute in templateAttributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS))
            {
                aliasAttribute.DataType = null;
                aliasAttribute.DecimalPlace = null;
                aliasAttribute.ThousandSeparator = null;
                aliasAttribute.Value = null;
                aliasAttribute.UomId = null;
            }
        }

        private async Task<string> DecorateNonRuntimeAttributeAsync(AssetAttributeDto attribute, Asset.AttributeMapping payload)
        {
            var newAttributeValue = attribute.Value?.ToString();

            switch (attribute.AttributeType)
            {
                case AttributeTypeConstants.TYPE_DYNAMIC:
                    await DecorateDynamicAttributeAsync(payload);
                    break;

                case AttributeTypeConstants.TYPE_INTEGRATION:
                    await DecorateIntegrationAttributeAsync(payload);
                    break;

                case AttributeTypeConstants.TYPE_COMMAND:
                    await DecorateCommandAttributeAsync(payload);
                    break;

                case AttributeTypeConstants.TYPE_STATIC:
                    if (payload != null && payload.TemplateAttributeId != Guid.Empty && payload.Value != null)
                    {
                        newAttributeValue = payload.Value?.ToString();
                    }
                    break;

                default:
                    break;
            }
            return newAttributeValue;
        }

        private async Task DecorateDynamicAttributeAsync(Asset.AttributeMapping attribute)
        {
            if (string.IsNullOrEmpty(attribute.DeviceId))
                throw new EntityInvalidException(detailCode: MessageConstants.ASSET_TEMPLATE_DECORATED_ATTRIBUTE_INVALID);

            var deviceId = attribute[AssetTemplateConstants.DEVICE_ID].ToString();
            // for dynamic type, need to add the deviceTemplateId into payload
            var deviceTemplateId = await _readDeviceRepository.AsQueryable().AsNoTracking().Where(x => x.Id == deviceId).Select(x => x.TemplateId).FirstAsync();
            attribute[AssetTemplateConstants.DEVICE_TEMPLATE_ID] = deviceTemplateId.ToString();
            if (!attribute.ContainsKey(AssetTemplateConstants.MARKUP_NAME))
            {
                attribute[AssetTemplateConstants.MARKUP_NAME] = $"Markup for {deviceId}";
            }
        }

        private async Task DecorateIntegrationAttributeAsync(Asset.AttributeMapping attribute)
        {
            if (string.IsNullOrEmpty(attribute.DeviceId) || string.IsNullOrEmpty(attribute.IntegrationId))
                throw new EntityInvalidException(detailCode: MessageConstants.ASSET_TEMPLATE_DECORATED_ATTRIBUTE_INVALID);

            var integrationId = Guid.Parse(attribute.IntegrationId);
            var deviceId = attribute[AssetTemplateConstants.DEVICE_ID].ToString();
            if (!attribute.ContainsKey(AssetTemplateConstants.DEVICE_MARKUP_NAME))
            {
                attribute[AssetTemplateConstants.DEVICE_MARKUP_NAME] = $"Device markup for {deviceId}";
            }
            if (!attribute.ContainsKey(AssetTemplateConstants.INTEGRATION_MARKUP_NAME))
            {
                // for integration type, need to add the IntegrationMarkupName and DeviceMarkupName into payload
                // query the integration detail
                var integrationMarkupName = await _readAssetAttributeTemplateRepository.AsQueryable().AsNoTracking().Where(x => x.AssetAttributeIntegration.IntegrationId == integrationId)
                .Select(x => x.AssetAttributeIntegration.IntegrationMarkupName).FirstOrDefaultAsync();
                if (string.IsNullOrEmpty(integrationMarkupName))
                {
                    // need to find the integration detail in broker.
                    var brokerService = _clientFactory.CreateClient(HttpClientNames.BROKER, _tenantContext);
                    var integrationResponse = await brokerService.GetAsync($"bkr/integrations/{integrationId}");
                    integrationResponse.EnsureSuccessStatusCode();
                    var integrationContent = await integrationResponse.Content.ReadAsByteArrayAsync();
                    var integrationDetail = integrationContent.Deserialize<IntegrationDto>();
                    integrationMarkupName = $"Integration markup for {integrationDetail.Name}";
                }
                attribute[AssetTemplateConstants.INTEGRATION_MARKUP_NAME] = integrationMarkupName;
            }
        }

        private async Task DecorateCommandAttributeAsync(Asset.AttributeMapping attribute)
        {
            //command attribute use same rules as dynamic attribute
            if (string.IsNullOrEmpty(attribute.DeviceId))
                throw new EntityInvalidException(detailCode: MessageConstants.ASSET_TEMPLATE_DECORATED_ATTRIBUTE_INVALID);

            var deviceId = attribute[AssetTemplateConstants.DEVICE_ID].ToString();
            var deviceTemplateId = await _readDeviceRepository.AsQueryable().AsNoTracking().Where(x => x.Id == deviceId).Select(x => x.TemplateId).FirstAsync();
            attribute[AssetTemplateConstants.DEVICE_TEMPLATE_ID] = deviceTemplateId.ToString();
            if (!attribute.ContainsKey(AssetTemplateConstants.MARKUP_NAME))
            {
                attribute[AssetTemplateConstants.MARKUP_NAME] = $"Markup for {deviceId}";
            }
        }

        public async Task<UpdateAssetTemplateDto> UpdateAssetTemplateAsync(UpdateAssetTemplate command, CancellationToken cancellationToken)
        {
            Domain.Entity.AssetTemplate dbEntity = null;
            Task<long[]> upsertTagsTask = null;
            command.Upn = _userContext.Upn;
            command.ApplicationId = Guid.Parse(!string.IsNullOrEmpty(_userContext.ApplicationId) ? _userContext.ApplicationId : ApplicationInformation.APPLICATION_ID);

            if (command.Tags != null && command.Tags.Any())
            {
                upsertTagsTask = _tagService.UpsertTagsAsync(command);
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                dbEntity = await ProcessUpdateAsync(command, cancellationToken);

                var isSameTags = command.IsSameTags(dbEntity.EntityTags);
                if (!isSameTags) //When tags difference
                {
                    await _unitOfWork.EntityTags.RemoveByEntityIdAsync(EntityTypeConstants.ASSET_TEMPLATE, dbEntity.Id, isTracking: true);

                    var tagIds = Array.Empty<long>();
                    if (upsertTagsTask != null)
                        tagIds = await upsertTagsTask;

                    if (tagIds.Any())
                    {
                        var entitiesTags = tagIds.Distinct().Select(x => new EntityTagDb
                        {
                            EntityType = EntityTypeConstants.ASSET_TEMPLATE,
                            EntityIdGuid = command.Id,
                            TagId = x
                        }).ToArray();

                        await _unitOfWork.EntityTags.AddRangeWithSaveChangeAsync(entitiesTags);
                    }
                }
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_TEMPLATE, ActionType.Update, ActionStatus.Fail, command.Id, command.Name, payload: command);
                throw;
            }
            await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_TEMPLATE, ActionType.Update, ActionStatus.Success, command.Id, command.Name, command);

            // send unlock request
            await _entityLockService.AcceptEntityUnlockRequestAsync(new EntityLock.Command.AcceptEntityUnlockRequestCommand()
            {
                TargetId = command.Id
            }, cancellationToken);

            // clean cache
            var assetAttributes = await (from asset in _readAssetRepository.OnlyAssetAsQueryable().AsNoTracking()
                                         join templateAttribute in _readAssetAttributeTemplateRepository.AsQueryable().AsNoTracking() on asset.AssetTemplateId equals templateAttribute.AssetTemplateId
                                         where templateAttribute.AssetTemplateId == command.Id && templateAttribute.AttributeType == AttributeTypeConstants.TYPE_RUNTIME
                                         select new AssetAttributeDto() { AssetId = asset.Id, Id = templateAttribute.Id, AttributeType = templateAttribute.AttributeType }
                             ).ToListAsync();
            await _deviceBackgroundService.QueueAsync(_tenantContext, new CleanAssetCache(assetAttributes));

            // Send event asset template changed
            await _dispatcher.SendAsync(new AssetTemplateChangedEvent(command.Id, _tenantContext));

            return await _tagService.FetchTagsAsync(UpdateAssetTemplateDto.Create(dbEntity));
        }

        public async Task<bool> RemoveAssetTemplateAsync(DeleteAssetTemplate command, CancellationToken cancellationToken)
        {
            if (!command.Ids.Any())
                return true;
            var deleteTemplateNames = new List<string>();
            var assetIds = new List<Guid>();
            EntityChangedNotificationMessage entityChangedNotification = new EntityChangedNotificationMessage();
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await ValidateIfEntitiesLockedAsync(command.Ids, cancellationToken);
                foreach (var assetTemplateId in command.Ids)
                {
                    var assetUsingTemplates = await _readAssetRepository.OnlyAssetAsQueryable().AsNoTracking()
                                                                      .Include(x => x.Attributes)
                                                                      .Include(x => x.AssetTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AssetAttributeDynamic)
                                                                      .Include(x => x.AssetTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AssetAttributeIntegration)
                                                                      .Include(x => x.AssetTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AssetAttributeRuntime)
                                                                      .Include(x => x.AssetTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AssetAttributeCommand)
                                                                      .Include(x => x.AssetAttributeDynamicMappings)
                                                                      .Include(x => x.AssetAttributeIntegrationMappings)
                                                                      .Include(x => x.AssetAttributeRuntimeMappings)
                                                                      .Include(x => x.AssetAttributeStaticMappings)
                                                                      .Include(x => x.AssetAttributeCommandMappings)
                                                                      .Include(x => x.AssetAttributeAliasMappings)
                                                                      .Where(x => x.AssetTemplateId == assetTemplateId).ToListAsync();

                    if (!assetUsingTemplates.Any())
                        continue;

                    foreach (var asset in assetUsingTemplates)
                    {
                        assetIds.Add(asset.Id);
                        await ProcessAddAttributeStandaloneAsync(asset);
                    }
                }

                await _unitOfWork.EntityTags.RemoveByEntityIdsAsync(EntityTypeConstants.ASSET_TEMPLATE, command.Ids.ToList());

                var (_, deletedNames) = await ProcessRemoveAsync(command, entityChangedNotification);
                deleteTemplateNames.AddRange(deletedNames);
                await _unitOfWork.CommitAsync();
            }
            catch (System.Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_TEMPLATE, ActionType.Delete, ex, command.Ids, deleteTemplateNames, payload: command);
                throw;
            }

            foreach (var notificationMessage in entityChangedNotification.Items)
            {
                await _notificationService.SendAssetNotifyAsync(new AssetNotificationMessage(notificationMessage.Id, NotificationType.ASSET_CHANGE, notificationMessage));
            }

            // send unlock request
            await _entityLockService.AcceptEntityUnlockRequestAsync(new EntityLock.Command.AcceptEntityUnlockRequestCommand()
            {
                TargetId = command.Id
            }, cancellationToken);

            await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_TEMPLATE, ActionType.Delete, ActionStatus.Success, command.Ids, deleteTemplateNames, payload: command);

            var attributes = assetIds.Select(x => new AssetAttributeDto { AssetId = x }).ToList();
            if (attributes.Any())
            {
                var deleteFields = attributes.Select(x =>
                {
                    return CacheKey.ASSET_HASH_FIELD.GetCacheKey(x.AssetId);
                });

                if (deleteFields.Any())
                {
                    var hashKey = CacheKey.ASSET_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);
                    await _cache.DeleteHashByKeysAsync(hashKey, deleteFields.ToList());
                }

                var attributesHashKey = CacheKey.ALL_ALIAS_REFERENCE_ATTRIBUTES_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);
                var attributeIdsHashKey = CacheKey.ALIAS_REFERENCE_ID_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);
                await _cache.ClearHashAsync(attributesHashKey);
                await _cache.ClearHashAsync(attributeIdsHashKey);
                await _deviceBackgroundService.QueueAsync(_tenantContext, new CleanAssetCache(attributes));
            }

            return true;
        }

        private async Task ProcessAddAttributeStandaloneAsync(Domain.Entity.Asset asset)
        {
            var assetDto = GetAssetDto.Create(asset);
            var attributes = asset.Attributes.Select(attr => JObject.FromObject(attr).ToObject<Asset.Command.AssetAttribute>());
            foreach (var attrTemplate in asset.AssetTemplate.Attributes)
            {
                var mapping = new AttributeStandaloneMapping();
                switch (attrTemplate.AttributeType)
                {
                    case AttributeTypeConstants.TYPE_STATIC:
                        {
                            var attrMapping = asset.AssetAttributeStaticMappings.First(x => x.AssetAttributeTemplateId == attrTemplate.Id);
                            mapping = new AttributeStandaloneMapping(attrMapping.Id, attrMapping.AssetId, attrMapping.AssetAttributeTemplateId);
                            break;
                        }
                    case AttributeTypeConstants.TYPE_DYNAMIC:
                        {
                            var attrMapping = asset.AssetAttributeDynamicMappings.First(x => x.AssetAttributeTemplateId == attrTemplate.Id);
                            mapping = new AttributeStandaloneMapping(attrMapping.Id, attrMapping.AssetId, attrMapping.AssetAttributeTemplateId);
                            break;
                        }
                    case AttributeTypeConstants.TYPE_RUNTIME:
                        {
                            var attrMapping = asset.AssetAttributeRuntimeMappings.First(x => x.AssetAttributeTemplateId == attrTemplate.Id);
                            mapping = new AttributeStandaloneMapping(attrMapping.Id, attrMapping.AssetId, attrMapping.AssetAttributeTemplateId);
                            break;
                        }
                    case AttributeTypeConstants.TYPE_INTEGRATION:
                        {
                            var attrMapping = asset.AssetAttributeIntegrationMappings.First(x => x.AssetAttributeTemplateId == attrTemplate.Id);
                            mapping = new AttributeStandaloneMapping(attrMapping.Id, attrMapping.AssetId, attrMapping.AssetAttributeTemplateId);
                            break;
                        }
                    case AttributeTypeConstants.TYPE_COMMAND:
                        {
                            var attrMapping = asset.AssetAttributeCommandMappings.First(x => x.AssetAttributeTemplateId == attrTemplate.Id);
                            mapping = new AttributeStandaloneMapping(attrMapping.Id, attrMapping.AssetId, attrMapping.AssetAttributeTemplateId);
                            break;
                        }
                    case AttributeTypeConstants.TYPE_ALIAS:
                        {
                            var attrMapping = asset.AssetAttributeAliasMappings.First(x => x.AssetAttributeTemplateId == attrTemplate.Id);
                            mapping = new AttributeStandaloneMapping(attrMapping.Id, attrMapping.AssetId, attrMapping.AssetAttributeTemplateId);
                            break;
                        }
                }

                var newAttribute = new Asset.Command.AssetAttribute
                {
                    Id = mapping.Id,
                    AssetId = mapping.AssetId,
                    Name = attrTemplate.Name,
                    Value = attrTemplate.Value,
                    AttributeType = attrTemplate.AttributeType,
                    DataType = attrTemplate.DataType,
                    UomId = attrTemplate.UomId,
                    DecimalPlace = attrTemplate.DecimalPlace,
                    ThousandSeparator = attrTemplate.ThousandSeparator,
                    Payload = assetDto.Attributes.First(x => x.Id == mapping.Id).Payload,
                    IsStandalone = true
                };

                try
                {
                    await _assetAttributeHandler.AddAsync(newAttribute, attributes, CancellationToken.None, ignoreValidation: true);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError($"Add asset standalone failed - payload {newAttribute.ToJson()}");
                    _logger.LogError(ex, ex.Message);
                    throw ex;
                }
            }
        }

        public async Task<ActivityResponse> ExportAssetTemplateAsync(ExportAssetTemplate request, CancellationToken cancellationToken)
        {
            try
            {
                var ids = request.Ids.Select(x => new Guid(x));
                var existingEntityCount = await _readAssetTemplateRepository.AsQueryable().CountAsync(x => ids.Contains(x.Id));
                if (existingEntityCount < ids.Count())
                {
                    throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
                }
                await _fileEventService.SendExportEventAsync(request.ActivityId, request.ObjectType, request.Ids);
                return new ActivityResponse(request.ActivityId);
            }
            catch (System.Exception ex)
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_TEMPLATE, ActionType.Export, exception: ex, payload: request);
                throw;
            }
        }

        public async Task<ActivityResponse> ExportAssetTemplateAttributeAsync(ExportAssetTemplateAttribute request, CancellationToken cancellationToken)
        {
            try
            {
                var ids = request.Ids.Select(x => new Guid(x));
                var existingEntityCount = await _readAssetTemplateRepository.AsQueryable().CountAsync(x => ids.Contains(x.Id));
                if (existingEntityCount < ids.Count())
                {
                    throw new EntityNotFoundException();
                }
                await _fileEventService.SendExportEventAsync(request.ActivityId, request.ObjectType, request.Ids);
                return new ActivityResponse(request.ActivityId);
            }
            catch (System.Exception ex)
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_TEMPLATE_ATTRIBUTE, ActionType.Export, exception: ex, payload: request);
                throw;
            }
        }

        private async Task ProcessAddAsync(GetAssetTemplateDto assetTemplate, AddAssetTemplate command, CancellationToken cancellationToken)
        {
            //process add attribute: wrap to jsonPath for add in property service
            if (command.Attributes == null || !command.Attributes.Any())
                return;
            var addJsonPath = new JsonPatchDocument();
            var attributes = new List<AssetAttributeTemplate.Command.Model.GetAssetAttributeTemplateDto>();
            foreach (var at in command.Attributes)
            {
                var path = "/";
                at.AssetTemplate = assetTemplate;
                addJsonPath.Add(path, at);
                attributes.Add(AssetAttributeTemplate.Command.Model.GetAssetAttributeTemplateDto.Create(AssetTemplateAttribute.Create(at)));
            }

            assetTemplate.Attributes = attributes;
            if (!await IsExistsMetricAsync(addJsonPath))
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.TemplateDetail.Key));

            //add json patch attribute template
            _ = await _assetAttributeTemplateService.UpsertAssetAttributeTemplateAsync(addJsonPath, assetTemplate, cancellationToken);
        }

        private async Task<Domain.Entity.AssetTemplate> ProcessUpdateAsync(UpdateAssetTemplate command, CancellationToken cancellationToken)
        {
            await ValidateIfEntityLockedAsync(command.Id, cancellationToken);

            var entityDB = await _unitOfWork.Templates.AsQueryable().Where(x => x.Id == command.Id).FirstOrDefaultAsync();
            if (entityDB == null)
                throw new EntityNotFoundException();

            if (await IsDuplicationAssetTemplateAsync(command.Name, command.Id))
                throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(Domain.Entity.AssetTemplate.Name));

            var attributeNameField = $"{nameof(UpdateAssetTemplate.Attributes)}.{nameof(AssetTemplateAttribute.Name)}";
            if (await IsDuplicationAttributeNameAsync(entityDB.Id, command.Attributes))
                throw ValidationExceptionHelper.GenerateDuplicateValidation(attributeNameField);

            if (!await IsExistsMetricAsync(command.Attributes))
                throw ValidationExceptionHelper.GenerateNotFoundValidation(attributeNameField);

            var entity = UpdateAssetTemplate.Create(command);
            entity = await _unitOfWork.Templates.UpdateEntityAsync(entity);

            //update attribute
            //add json patch attribute template
            _ = await _assetAttributeTemplateService.UpsertAssetAttributeTemplateAsync(command.Attributes, GetAssetTemplateDto.Create(entity), cancellationToken);

            return entityDB;
        }

        private async Task<(BaseResponse, IEnumerable<string>)> ProcessRemoveAsync(DeleteAssetTemplate command, EntityChangedNotificationMessage changedNotification)
        {
            if (command.Ids == null || !command.Ids.Any())
                return (BaseResponse.Success, Enumerable.Empty<string>());

            var assetTemplateIds = command.Ids.Distinct().ToArray();
            //list assetTemplate
            var deletedAssets = await _unitOfWork.Templates.AsQueryable().Where(x => assetTemplateIds.Contains(x.Id)).ToListAsync();
            var deleteTemplateNames = deletedAssets.Select(x => x.Name).ToList();
            if (assetTemplateIds.Count() > deletedAssets.Count())
            {
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
            }

            foreach (var assetTemplateId in command.Ids)
            {
                var deleteAsset = deletedAssets.FirstOrDefault(x => x.Id == assetTemplateId);
                if (deleteAsset is null)
                    continue;

                var entityTracking = await _unitOfWork.Templates.AsQueryable().FirstOrDefaultAsync(x => x.Id == assetTemplateId);
                if (entityTracking == null)
                    throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);

                _ = await _unitOfWork.Templates.RemoveEntityAsync(entityTracking);
                changedNotification.AddItem(EntityType.Asset, deleteAsset.Id, deleteAsset.Name, EntityChangedAction.Delete, _userContext.Upn);
            }

            return (BaseResponse.Success, deleteTemplateNames);
        }
        private async Task<bool> IsExistsMetricAsync(JsonPatchDocument document)
        {
            var metricKeys = new List<string>();

            List<Operation> operations = document.Operations;
            foreach (Operation operation in operations)
            {
                // remove operation -> operation.value should be null -> skipp this check
                if (operation.op == "remove")
                {
                    continue;
                }
                var updateAttribute = JsonConvert.DeserializeObject<AssetTemplateAttribute>(JsonConvert.SerializeObject(operation.value));
                if (updateAttribute == null)
                    continue;
                if (updateAttribute.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC || updateAttribute.AttributeType == AttributeTypeConstants.TYPE_COMMAND)
                {
                    var dynamicPayload = JObject.FromObject(updateAttribute.Payload).ToObject<AssetAttributeDynamicTemplate>();
                    var template = await _readDeviceTemplateRepository.AsQueryable().AsNoTracking().Include(x => x.Bindings).Include(x => x.Payloads).ThenInclude(x => x.Details).FirstOrDefaultAsync(x => x.Id == dynamicPayload.DeviceTemplateId);

                    if (template == null)
                        continue;
                    metricKeys.AddRange(template.Payloads.SelectMany(payload => payload.Details).Select(detail => detail.Key));
                    metricKeys.AddRange(template.Bindings.Select(binding => binding.Key));
                    if (!metricKeys.Contains(dynamicPayload.MetricKey))
                        return false;
                }
            }
            return true;
        }

        public async Task<bool> IsDuplicationAttributeNameAsync(Guid assetTemplateId, IEnumerable<string> attributeNames)
        {
            var assetsUsingTemplate = await _readAssetRepository.OnlyAssetAsQueryable().Where(x => x.AssetTemplateId == assetTemplateId).Include(x => x.Attributes).ToListAsync();

            if (assetsUsingTemplate != null && assetsUsingTemplate.Any())
            {
                foreach (var asset in assetsUsingTemplate)
                {
                    var assetAttribute = asset.Attributes.FirstOrDefault(x => attributeNames.Contains(x.Name));
                    if (assetAttribute != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private Task<bool> IsDuplicationAttributeNameAsync(Guid assetTemplateId, JsonPatchDocument document)
        {
            List<Operation> operations = document.Operations;
            var attributeNames = new List<string>();
            foreach (Operation operation in operations)
            {
                if (operation.op == OperationConstants.ADD || operation.op == OperationConstants.EDIT)
                {
                    var updateAttribute = JsonConvert.DeserializeObject<AssetTemplateAttribute>(JsonConvert.SerializeObject(operation.value));
                    attributeNames.Add(updateAttribute.Name);
                }
            }
            return IsDuplicationAttributeNameAsync(assetTemplateId, attributeNames);
        }

        private Task<bool> IsDuplicationAssetTemplateAsync(string name, Guid id = default)
        {
            return _readAssetTemplateRepository.AsQueryable().AsNoTracking().Where(x => x.Name.ToLower() == name.ToLower() && x.Id != id).AnyAsync();
        }

        private Task ValidateIfEntityLockedAsync(Guid assetId, CancellationToken cancellationToken)
        {
            var lstEntitiesId = new List<Guid> { assetId };
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

        public Task<BaseResponse> CheckExistingAssetTemplateAsync(CheckExistingAssetTemplate command, CancellationToken cancellationToken)
        {
            return ValidateExistAssetTemplateAsync(command, cancellationToken);
        }

        private async Task<BaseResponse> ValidateExistAssetTemplateAsync(CheckExistingAssetTemplate command, CancellationToken cancellationToken)
        {
            var requestIds = new HashSet<Guid>(command.Ids.Distinct());
            var templates = new HashSet<Guid>(await _readAssetTemplateRepository.AsQueryable().AsNoTracking().Where(x => requestIds.Contains(x.Id)).Select(x => x.Id).ToListAsync());
            if (!requestIds.SetEquals(templates))
                throw new EntityNotFoundException();
            return BaseResponse.Success;
        }

        public async Task<IEnumerable<ArchiveAssetTemplateDto>> ArchiveAsync(ArchiveAssetTemplate command, CancellationToken cancellationToken)
        {
            var assetTemplates = await _readAssetTemplateRepository.AsQueryable().AsNoTracking()
                                            .Include(a => a.Attributes).ThenInclude(a => a.AssetAttributeIntegration)
                                            .Include(a => a.Attributes).ThenInclude(a => a.AssetAttributeDynamic)
                                            .Include(a => a.Attributes).ThenInclude(a => a.AssetAttributeRuntime)
                                            .Include(a => a.Attributes).ThenInclude(a => a.AssetAttributeCommand)
                                            .Include(x => x.EntityTags)
                                            .Where(x => x.UpdatedUtc <= command.ArchiveTime && (!x.EntityTags.Any() || x.EntityTags.Any(a => a.EntityType == EntityTypeConstants.ASSET_TEMPLATE)))
                                            .Select(x => ArchiveAssetTemplateDto.CreateDto(x)).ToListAsync();
            return assetTemplates;
        }

        public async Task<BaseResponse> VerifyArchiveAsync(VerifyAssetTemplate command, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<ArchiveAssetTemplateDataDto>(command.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
            foreach (var assetTemplate in data.AssetTemplates)
            {
                var validation = await _templateVerifyValidator.ValidateAsync(assetTemplate);
                if (!validation.IsValid)
                {
                    throw EntityValidationExceptionHelper.GenerateException(nameof(command.Data), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
                }
            }
            return BaseResponse.Success;
        }

        public async Task<BaseResponse> RetrieveAsync(RetrieveAssetTemplate command, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<ArchiveAssetTemplateDataDto>(command.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
            if (!data.AssetTemplates.Any())
            {
                return BaseResponse.Success;
            }

            if (command.AdditionalData != null)
            {
                var integrationIds = command.AdditionalData.ToDictionary(x => Guid.Parse(x.Key), x => Guid.Parse(x.Value.ToString()));
                ProcessAssignIntegrationId(data, integrationIds);
            }
            _userContext.SetUpn(command.Upn);
            var entities = data.AssetTemplates.OrderBy(x => x.UpdatedUtc).Select(dto => ArchiveAssetTemplateDto.CreateEntity(dto, command.Upn)).ToList();
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Templates.RetrieveAsync(entities);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
            return BaseResponse.Success;
        }

        private void ProcessAssignIntegrationId(ArchiveAssetTemplateDataDto data, IDictionary<Guid, Guid> integrationIds)
        {
            // IntegrationId is regenerated on retrieve integration
            // So, we need to find the new ID from the list of given integration ID pairs.
            foreach (var item in data.AssetTemplates.SelectMany(x => x.Attributes.Where(att => att.AttributeType == AttributeTypeConstants.TYPE_INTEGRATION)))
            {
                if (item.Payload != null && item.Payload.TryGetValue("integrationId", out var value))
                {
                    var oldId = Guid.Parse(value as string);

                    if (integrationIds.TryGetValue(oldId, out var newId))
                    {
                        item.Payload["integrationId"] = newId.ToString();
                    }
                }
            }
        }

        public async Task<AttributeTemplateParsed> ParseAttributeTemplateAsync(ParseAttributeTemplate request, CancellationToken cancellationToken)
        {
            var response = new AttributeTemplateParsed();
            var client = _clientFactory.CreateClient(HttpClientNames.DEVICE_FUNCTION, _tenantContext);
            var body = new StringContent(JsonConvert.SerializeObject(new
            {
                FileName = request.FileName,
                ObjectType = request.ObjectType,
                TemplateId = request.TemplateId,
                Upn = _userContext.Upn,
                DateTimeFormat = _userContext.DateTimeFormat,
                DateTimeOffset = _userContext.Timezone?.Offset,
                UnsavedAttributes = request.UnsavedAttributes
            }), System.Text.Encoding.UTF8, "application/json");

            var responseMessage = await client.PostAsync($"fnc/dev/assettemplates/attributes/parse", body);
            responseMessage.EnsureSuccessStatusCode();
            var stream = await responseMessage.Content.ReadAsByteArrayAsync();
            var data = stream.Deserialize<AttributeTemplateParsedDto>();

            response.Errors = data.Errors;
            response.Attributes = data.Attributes.Select(x => AttributeParsed.Create(x));
            return response;
        }

        public async Task<BaseResponse> CheckUsingAttributeAsync(Guid attributeTemplateId)
        {
            var existAssetAttributeIds = await _unitOfWork.Attributes.GetAssetAttributeIdsAsync(new List<Guid>() { attributeTemplateId });
            var dependencyOfAttributesCreatedFromTemplate = Enumerable.Empty<AttributeDependency>();
            if (existAssetAttributeIds.Any())
            {
                dependencyOfAttributesCreatedFromTemplate = await _assetService.GetDependencyOfAttributeAsync(existAssetAttributeIds);
            }

            var dependencyInsideTemplate = await _unitOfWork.Attributes.GetDependenciesInsideTemplateAsync(attributeTemplateId);
            string templateName = string.Empty;
            Guid templateId = Guid.Empty;
            if (dependencyInsideTemplate.Any())
            {
                templateId = dependencyInsideTemplate.First().AssetTemplateId;
                templateName = (await _unitOfWork.Templates.FindAsync(templateId))?.Name;
            }
            var dependencies = dependencyOfAttributesCreatedFromTemplate.Union(dependencyInsideTemplate.Select(x => AttributeDependency.Create(DependencyType.ASSET_TEMPLATE_ATTRIBUTE, templateId, $"{templateName}.{x.Name}")));

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

            return BaseResponse.Success;
        }

        public async Task<BaseResponse> CheckUsingAssetTemplateAsync(Guid templateId)
        {
            var existTemplate = await _unitOfWork.Templates.AsQueryable()
                                                    .Include(x => x.Assets)
                                                    .AsNoTracking()
                                                    .FirstOrDefaultAsync(x => x.Id == templateId);
            if (existTemplate == null)
                throw new EntityNotFoundException();

            var dependencies = existTemplate.Assets.Select(x => AssetDependency.Create(DependencyType.ASSET, x.Id, x.Name));
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

            return BaseResponse.Success;
        }

        public async Task<ValidateAssetResponse> ValidateDeleteTemplateAsync(Guid attributeTemplateId)
        {
            var existTemplate = await _readAssetTemplateRepository.AsQueryable().AsNoTracking().AnyAsync(x => x.Id == attributeTemplateId);
            if (!existTemplate)
                throw new EntityNotFoundException();
            try
            {
                await CheckUsingAssetTemplateAsync(attributeTemplateId);
            }
            catch (EntityValidationException ex)
            {
                return new ValidateAssetResponse(false, ex.ErrorCode, ex.Payload);
            }

            return ValidateAssetResponse.Success;
        }

        public override async Task<BaseSearchResponse<GetAssetTemplateDto>> SearchAsync(GetAssetTemplateByCriteria criteria)
        {
            criteria.MappingSearchTags();
            var response = await base.SearchAsync(criteria);
            return await _tagService.FetchTagsAsync(response);
        }
    }
}