using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.MultiTenancy.Internal;
using AHI.Infrastructure.Service;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using AHI.Infrastructure.UserContext.Abstraction;
using Device.Application.Asset.Command;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Device.Command;
using Device.Application.Device.Command.Model;
using Device.Application.Events;
using Device.Application.Model;
using Device.Application.Models;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.ApplicationExtension.Extension;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using AHI.Infrastructure.Service.Tag.Extension;
using Device.Domain.Entity;
using AHI.Infrastructure.SharedKernel.Models;
using AHI.Infrastructure.Service.Tag.Enum;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Device.Application.Service
{
    public class DeviceService : BaseSearchService<Domain.Entity.Device, string, GetDeviceByCriteria, GetDeviceDto>, IDeviceService
    {
        private readonly IDomainEventDispatcher _dispatcher;
        private readonly ITenantContext _tenantContext;
        private readonly IFileEventService _fileEventService;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserContext _userContext;
        private readonly DeviceBackgroundService _deviceBackgroundService;
        private readonly ILoggerAdapter<DeviceService> _logger;
        private readonly IDeviceUnitOfWork _deviceUnitOfWork;
        private readonly IValidator<ArchiveDeviceDto> _validator;
        private readonly IExportNotificationService _notification;
        private readonly IReadDeviceRepository _readDeviceRepository;
        private readonly IReadAssetAttributeRepository _readAssetAttributeRepository;
        private readonly IReadAssetAttributeTemplateRepository _readAssetAttributeTemplateRepository;
        private readonly IReadValidTemplateRepository _readValidTemplateRepository;
        private readonly ICache _cache;
        private readonly ITagService _tagService;

        private static readonly string[] VALID_BROKER_DETAIL_KEYS =
        {
            "brokerId",
            "brokerType",
            "enable_sharing",
            "retentionDays",
            "sasTokenDuration",
            "tokenDuration",
            "deviceId",
        };

        private const string DEFAULT_TOPIC = "$ahi/telemetry";
        private const int DEFAULT_PASSWORD_LENGTH = 30;

        public DeviceService(
                IServiceProvider serviceProvider,
                IDomainEventDispatcher dispatcher,
                ITenantContext tenantContext,
                IFileEventService fileEventService,
                IHttpClientFactory httpClientFactory,
                IAuditLogService auditLogService,
                IUserContext userContext,
                IDeviceUnitOfWork deviceUnitOfWork,
                DeviceBackgroundService deviceBackgroundService,
                ILoggerAdapter<DeviceService> logger,
                IValidator<ArchiveDeviceDto> validator,
                IExportNotificationService notification,
                IReadDeviceRepository readDeviceRepository,
                IReadAssetAttributeRepository readAssetAttributeRepository,
                IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository,
                IReadValidTemplateRepository readValidTemplateRepository,
                ICache cache,
                ITagService tagService
            ) : base(GetDeviceDto.Create, serviceProvider)
        {
            _dispatcher = dispatcher;
            _tenantContext = tenantContext;
            _fileEventService = fileEventService;
            _auditLogService = auditLogService;
            _httpClientFactory = httpClientFactory;
            _userContext = userContext;
            _deviceUnitOfWork = deviceUnitOfWork;
            _deviceBackgroundService = deviceBackgroundService;
            _logger = logger;
            _validator = validator;
            _notification = notification;
            _readDeviceRepository = readDeviceRepository;
            _readAssetAttributeRepository = readAssetAttributeRepository;
            _readAssetAttributeTemplateRepository = readAssetAttributeTemplateRepository;
            _readValidTemplateRepository = readValidTemplateRepository;
            _cache = cache;
            _tagService = tagService;
        }

        protected override Type GetDbType()
        {
            return typeof(IDeviceRepository);
        }

        public async Task<FetchDeviceMetricDto> FetchDeviceMetricAsync(string deviceId, int metricId, string metricKey)
        {
            var metric = await _deviceUnitOfWork.TemplateDetailRepository.AsFetchDeviceMetricQueryable(deviceId, metricId, metricKey).FirstOrDefaultAsync();
            return FetchDeviceMetricDto.Create(metric);
        }

        private async Task<BrokerDto> GetBrokerByIdAsync(Guid brokerId, string projectId)
        {
            var tenantContext = new TenantContext();
            tenantContext.RetrieveFromString(_tenantContext.TenantId, _tenantContext.SubscriptionId, projectId);
            var client = _httpClientFactory.CreateClient(HttpClientNames.BROKER, tenantContext);
            var response = await client.GetAsync($"bkr/brokers/{brokerId}");
            if (!response.IsSuccessStatusCode)
            {
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.NotFound:
                        throw new EntityNotFoundException(detailCode: MessageConstants.DEVICE_IOT_HUB_BROKER_DELETED);
                    default:
                        var message = await response.Content.ReadAsStringAsync();
                        throw new SystemCallServiceException(message: message, detailCode: MessageConstants.BLOCK_EXECUTION_ERROR_CALL_SERVICE);
                }
            }
            var content = await response.Content.ReadAsByteArrayAsync();
            return content.Deserialize<BrokerDto>();
        }

        private async Task<IEnumerable<SharedBrokerDto>> GetAllAvailableBrokersAsync()
        {
            var client = _httpClientFactory.CreateClient(HttpClientNames.PROJECT, _tenantContext);
            var searchContent = new StringContent(JsonConvert.SerializeObject(new
            {
                PageSize = int.MaxValue
            }), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"prj/brokers/search", searchContent);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsByteArrayAsync();
            return content.Deserialize<BaseSearchResponse<SharedBrokerDto>>().Data;
        }

        public async Task<AddDeviceDto> AddAsync(AddDevice payload, CancellationToken token)
        {
            await _deviceUnitOfWork.BeginTransactionAsync();
            DeviceConnectionChangedEvent deviceConnectionChanged = null;
            try
            {
                if (!string.IsNullOrEmpty(payload.Id) && await _deviceUnitOfWork.Devices.IsDuplicateDeviceIdAsync(payload.Id))
                {
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(Domain.Entity.Device.Id));
                }

                if (await IsTemplateDeleteAsync(payload.TemplateId))
                {
                    throw EntityValidationExceptionHelper.GenerateException(nameof(Domain.Entity.Device.TemplateId), MessageConstants.ERROR_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED, detailCode: MessageConstants.DETAIL_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED);
                }

                var entity = AddDevice.Create(payload, _tenantContext.ProjectId);
                await DecorateDeviceAsync(entity);
                entity.CreatedBy = !string.IsNullOrWhiteSpace(entity.CreatedBy) && _userContext.Upn == NameConstants.SYSTEM_UPN ? entity.CreatedBy : _userContext.Upn;
                var entityResult = await _deviceUnitOfWork.Devices.AddAsync(entity);
                deviceConnectionChanged = new DeviceConnectionChangedEvent(entityResult.Id, entityResult.Status, _tenantContext);
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
                        EntityType = Privileges.Device.ENTITY_NAME,
                        EntityIdString = entityResult.Id,
                        TagId = x
                    }).ToArray();

                    await _deviceUnitOfWork.EntityTags.AddRangeWithSaveChangeAsync(entitiesTags);
                }

                await _deviceUnitOfWork.CommitAsync();
                await HandleRedisCacheWhenAddEntityAsync(entityResult);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Error in DeviceService - AddAsync: {ex.Message}");
                await _deviceUnitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActionType.Add, ActionStatus.Fail, payload.Id, payload: new { Request = payload, Message = MessageDetailConstants.BROKER_HAS_BEEN_DELETED });
                throw EntityValidationExceptionHelper.GenerateException(nameof(BrokerIotDeviceDto.BrokerId), MessageConstants.ERROR_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED, detailCode: MessageConstants.DETAIL_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED);
            }
            catch (EntityValidationException ex1)
            {
                _logger.LogError(ex1, $"Error in DeviceService - AddAsync: {ex1.Message}");
                await _deviceUnitOfWork.RollbackAsync();
                var customException = CreateCustomException(ex1, nameof(payload.Id), payload.Id);
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActionType.Add, exception: customException, entityId: payload.Id, payload: payload);
                throw;
            }
            catch (System.Exception ex2)
            {
                _logger.LogError(ex2, $"Error in DeviceService - AddAsync: {ex2.Message}");
                await _deviceUnitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActionType.Add, exception: ex2, entityId: payload.Id, payload: payload);
                throw;
            }

            // connection leak issue, refactor to use the dapper instead
            int totalDevice = await _deviceUnitOfWork.Devices.GetTotalDeviceAsync();
            var deviceAdded = new DeviceChangedEvent(payload.Id, totalDevice, false, _tenantContext);
            await _dispatcher.SendAsync(deviceAdded);
            await _dispatcher.SendAsync(deviceConnectionChanged);
            await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActionType.Add, ActionStatus.Success, payload.Id, payload.Name, payload);

            // Delete the redis cache.
            await _deviceBackgroundService.QueueAsync(_tenantContext, new CleanDeviceCache(new[] { payload.Id }));

            return await _tagService.FetchTagsAsync(new AddDeviceDto() { Id = payload.Id });
        }

        private EntityValidationException CreateCustomException(
            EntityValidationException exc,
            string propertyName,
            string propertyValue)
        {
            var failures = exc.Failures.Select(x => new ValidationFailure(x.Key, x.Value[0])
            {
                FormattedMessagePlaceholderValues = new Dictionary<string, object>
                {
                    { "propertyName", propertyName },
                    { "propertyValue", propertyValue }
                }
            }).ToList();
            var customException = new EntityValidationException(failures: failures, exc.Message, exc.DetailCode, exc.Payload);
            if (customException.Failures.ContainsKey(propertyName) &&
                customException.Failures[propertyName][0] == MessageConstants.DEVICE_ID_UNMATCHED_BROKER_TYPE)
            {
                customException.Failures[propertyName][0] = MessageConstants.DEVICE_ID_UNMATCHED_BROKER_TYPE_LOG;
            }
            return customException;
        }

        private async Task DecorateDeviceAsync(Domain.Entity.Device device)
        {
            try
            {
                if (device.DeviceContent == null)
                {
                    if (await _deviceUnitOfWork.DeviceTemplates.HasBindingAsync(device.TemplateId))
                    {
                        throw ValidationExceptionHelper.GenerateRequiredValidation(nameof(BrokerIotDeviceDto.BrokerId));
                    }
                    else
                    {
                        return;
                    }
                }
                var payload = JsonConvert.DeserializeObject<BrokerIotDeviceDto>(device.DeviceContent);
                payload[DeviceContentPropertyConstants.DEVICE_ID] = device.Id;
                SafetyRemove(payload, DeviceContentPropertyConstants.ID);
                SafetyRemove(payload, DeviceContentPropertyConstants.NAME);
                SafetyRemove(payload, DeviceContentPropertyConstants.TAGS);
                SafetyRemove(payload, DeviceContentPropertyConstants.TEMPLATE_ID);
                SafetyRemove(payload, DeviceContentPropertyConstants.TEMPLATE_NAME);
                SafetyRemove(payload, DeviceContentPropertyConstants.STATUS);
                SafetyRemove(payload, DeviceContentPropertyConstants.BROKER);
                SafetyRemove(payload, DeviceContentPropertyConstants.CREATED_UTC);
                SafetyRemove(payload, DeviceContentPropertyConstants.UPDATED_UTC);
                SafetyRemove(payload, DeviceContentPropertyConstants.DESCRIPTION);
                SafetyRemove(payload, DeviceContentPropertyConstants.DEVICE_TEMPLATES);

                if (payload.BrokerId.HasValue)
                {
                    bool triggerListener = false;
                    var projectId = payload.ProjectId;
                    var broker = await GetBrokerByIdAsync(payload.BrokerId.Value, payload.ProjectId);
                    var brokerContent = JsonConvert.DeserializeObject<BrokerContentDto>(broker.Content);
                    if (broker.Status != BrokerStatus.ACTIVE)
                    {
                        throw EntityValidationExceptionHelper.GenerateException(nameof(BrokerDto.Status), MessageConstants.ERROR_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED, detailCode: MessageConstants.DETAIL_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED);
                    }
                    // need to validate the sharing of broker
                    // https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/2810
                    if (projectId != _tenantContext.ProjectId && !brokerContent.EnableSharing)
                    {
                        throw EntityValidationExceptionHelper.GenerateException(nameof(BrokerIotDeviceDto.BrokerId), MessageConstants.ERROR_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED, detailCode: MessageConstants.DETAIL_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED);
                    }

                    if (BrokerConstants.EMQX_BROKERS.Contains(broker.Type))
                    {
                        await ProccessEmqxBrokerAsync(device, broker, brokerContent, payload);
                    }
                    else
                    {
                        triggerListener = brokerContent.EnableSharing;
                        await ProccessIotHubBrokerAsync(device, broker, brokerContent, payload);
                    }

                    device.DeviceContent = JsonConvert.SerializeObject(payload);
                    if (triggerListener)
                        await _dispatcher.SendAsync(new BrokerChangedEvent(broker.Id, _tenantContext, true, AHI.Infrastructure.Bus.ServiceBus.Enum.ActionTypeEnum.Created));
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error in DeviceService - DecorateDeviceAsync: {ex.Message}");
                throw;
            }
        }

        private async Task ProccessIotHubBrokerAsync(
            Domain.Entity.Device device,
            BrokerDto broker,
            BrokerContentDto brokerContent,
            BrokerIotDeviceDto payload)
        {
            var brokerFunction = _httpClientFactory.CreateClient(HttpClientNames.BROKER_FUNCTION, _tenantContext);
            var tokenResponse = await brokerFunction.PostAsync("fnc/bkr/device/register", new StringContent(JsonConvert.SerializeObject(new Dictionary<string, object>(payload)
                {
                    {"tenantId",_tenantContext.TenantId.ToString()},
                    {"subscriptionId",_tenantContext.SubscriptionId.ToString()},
                    {"brokerContent",broker.Content}
                }), Encoding.UTF8, "application/json"));
            var responseContent = await tokenResponse.Content.ReadAsByteArrayAsync();

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorResponse = responseContent.Deserialize<BrokerErrorResposeDto>();
                if (errorResponse.DetailCode.Equals(ExceptionErrorCode.ERROR_ENTITY_VALIDATION) && errorResponse.Failures.ContainsKey("DeviceId"))
                {
                    if (errorResponse.Failures["DeviceId"].Contains(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_DUPLICATED))
                    {
                        throw EntityValidationExceptionHelper.GenerateException(nameof(device.Id), MessageConstants.DEVICE_IDENTIFIER_EXIST_IN_BROKER);
                    }
                    else
                    {
                        throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(device.Id), errorResponse.Failures["DeviceId"][0]);
                    }
                }
                throw new SystemCallServiceException(detailCode: errorResponse.Message);
            }
            var responsePayload = responseContent.Deserialize<DeviceRegisterResponseDto>();
            var tokenPayload = responsePayload.Payload;
            var primaryKey = tokenPayload.Authentication.SymmetricKey.PrimaryKey;
            payload["enable_sharing"] = brokerContent.EnableSharing.ToString().ToLowerInvariant();
            payload["primaryKey"] = primaryKey;
            payload["connectionString"] = GenerateConnectionString(broker.Content, device.Id, primaryKey);
            var duration = Convert.ToInt32(payload.ContainsKey("sasTokenDuration") ? payload["sasTokenDuration"] : "30");
            var resourceEndpoint = GetResoureEndpoint(broker.Content, device.Id);
            payload["sasToken"] = GenerateSASToken(resourceEndpoint, tokenPayload.Authentication.SymmetricKey.PrimaryKey, duration, null);
        }

        private async Task ProccessEmqxBrokerAsync(
            Domain.Entity.Device device,
            BrokerDto broker,
            BrokerContentDto brokerContent,
            BrokerIotDeviceDto payload)
        {
            string topic = "$ahi/telemetry";
            SafetyRemove(payload, "topic");
            SafetyRemove(payload, "username");
            SafetyRemove(payload, "secret");
            SafetyRemove(payload, "password");

            int expiry = 0;
            var res = int.TryParse(payload["tokenDuration"].ToString(), out expiry);
            if (!res || expiry < 1 || expiry > 3650)
                throw EntityValidationExceptionHelper.GenerateException("tokenDuration", ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);

            var topics = new List<string> { topic };
            if (!string.IsNullOrEmpty(device.TelemetryTopic))
                topics.Add(device.TelemetryTopic);
            if (device.HasCommand.HasValue && device.HasCommand.Value! && !string.IsNullOrEmpty(device.CommandTopic))
                topics.Add(device.CommandTopic);
            topics = topics.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

            await GenerateCredentialAsync(payload, expiry, brokerContent.PasswordLength);
            await UpdateBrokerInfoAsync(topics, broker.Id.ToString(), payload["username"].ToString(), payload["password"].ToString());
            await UpdateEmqxRedisCredentialAsync(topics, payload["username"].ToString(), payload["password"].ToString(), expiry);
        }

        private async Task GenerateCredentialAsync(BrokerIotDeviceDto payload, int expiry, int passwordLength)
        {
            var httpClient = _httpClientFactory.CreateClient(HttpClientNames.IDENTITY, _tenantContext);

            var clientInfo = await GetClientInfoAsync(httpClient, expiry, passwordLength);
            payload.TryAdd("username", clientInfo.Username);
            payload.TryAdd("password", clientInfo.Password);
        }

        private async Task<BrokerClientDto> GetClientInfoAsync(HttpClient idpClient, int expiry, int passwordLength)
        {
            var request = new
            {
                CreatedBy = _userContext.Upn,
                ExpiredDays = expiry,
                TenantId = _tenantContext.TenantId,
                ProjectId = _tenantContext.ProjectId,
                SubscriptionId = _tenantContext.SubscriptionId,
                PasswordLength = passwordLength == 0 ? DEFAULT_PASSWORD_LENGTH : passwordLength
            };
            HttpContent requestContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await idpClient.PostAsync($"idp/brokerclients", requestContent);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsByteArrayAsync();
            return content.Deserialize<BrokerClientDto>();
        }

        private async Task<string> RefreshEmqxBrokerTokenAsync(HttpClient idpClient, string username, string expiry, int passwordLength)
        {
            var request = new
            {
                CreatedBy = _userContext.Upn,
                ExpiredDays = expiry,
                PasswordLength = passwordLength == 0 ? DEFAULT_PASSWORD_LENGTH : passwordLength
            };

            HttpContent requestContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await idpClient.PutAsync($"idp/brokerclients/{username}", requestContent);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsByteArrayAsync();
            return content.Deserialize<BrokerClientDto>().Password;
        }

        public async Task<UpdateDeviceDto> RefreshTokenAsync(RefreshToken command, CancellationToken token)
        {
            await _deviceUnitOfWork.BeginTransactionAsync();
            var currentEntity = await _deviceUnitOfWork.Devices.FindAsync(command.Id);
            try
            {
                if (currentEntity == null)
                {
                    throw new EntityNotFoundException();
                }

                if (await IsTemplateDeleteAsync(currentEntity.TemplateId))
                {
                    throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.Device.TemplateId));
                }

                if (currentEntity.DeviceContent != null)
                {
                    var topics = GetDeviceTopics(currentEntity);
                    var content = JsonConvert.DeserializeObject<IDictionary<string, object>>(currentEntity.DeviceContent);
                    BrokerIotDeviceDto brokerIotDeviceDto = JsonConvert.DeserializeObject<BrokerIotDeviceDto>(currentEntity.DeviceContent);
                    int passwordLength = DEFAULT_PASSWORD_LENGTH;

                    if (brokerIotDeviceDto.BrokerId.HasValue)
                    {
                        var broker = await GetBrokerByIdAsync(brokerIotDeviceDto.BrokerId.Value, brokerIotDeviceDto.ProjectId);
                        var brokerContent = JsonConvert.DeserializeObject<BrokerContentDto>(broker.Content);
                        passwordLength = brokerContent.PasswordLength;
                    }

                    var idpClient = _httpClientFactory.CreateClient(HttpClientNames.IDENTITY, _tenantContext);
                    var newToken = await RefreshEmqxBrokerTokenAsync(idpClient, content["username"].ToString(), content["tokenDuration"].ToString(), passwordLength);
                    content["password"] = newToken;
                    currentEntity.DeviceContent = JsonConvert.SerializeObject(content);

                    await UpdateBrokerInfoAsync(topics, content["brokerId"].ToString(), content["username"].ToString(), newToken);
                    await UpdateEmqxRedisCredentialAsync(topics, content["username"].ToString(), newToken, 0, true);
                }

                _ = await _deviceUnitOfWork.Devices.UpdateAsync(command.Id, currentEntity);
                await _deviceUnitOfWork.CommitAsync();
            }
            catch
            {
                await _deviceUnitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActionType.Update, ActionStatus.Fail, command.Id, payload: command);
                throw;
            }

            return UpdateDeviceDto.Create(currentEntity);
        }

        private static IEnumerable<string> GetDeviceTopics(Domain.Entity.Device device)
        {
            var topics = new List<string> { DEFAULT_TOPIC };
            if (!string.IsNullOrEmpty(device.TelemetryTopic))
                topics.Add(device.TelemetryTopic);
            if (!string.IsNullOrEmpty(device.CommandTopic))
                topics.Add(device.CommandTopic);
            topics = topics.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
            return topics;
        }
        private async Task UpdateBrokerInfoAsync(
            IEnumerable<string> topics,
            string brokerId,
            string username,
            string password
        )
        {
            var brokerFunction = _httpClientFactory.CreateClient(HttpClientNames.BROKER_FUNCTION, _tenantContext);
            var assignResponse = await brokerFunction.PostAsync("fnc/bkr/emqx/assign/client", new StringContent(JsonConvert.SerializeObject(new
            {
                BrokerId = brokerId,
                ClientId = username,
                AccessToken = password,
                Topics = topics
            }), Encoding.UTF8, "application/json"));
            assignResponse.EnsureSuccessStatusCode();
        }

        private async Task UpdateBrokerInfoAsync(
            string topic,
            string brokerId,
            string username,
            string password
            )
        {
            var brokerFunction = _httpClientFactory.CreateClient(HttpClientNames.BROKER_FUNCTION, _tenantContext);
            var assignResponse = await brokerFunction.PostAsync("fnc/bkr/emqx/assign/client", new StringContent(JsonConvert.SerializeObject(new
            {
                BrokerId = brokerId,
                ClientId = username,
                AccessToken = password,
                Topic = topic
            }), Encoding.UTF8, "application/json"));
            assignResponse.EnsureSuccessStatusCode();
        }

        private async Task UpdateEmqxRedisCredentialAsync(
            IEnumerable<string> topics,
            string username,
            string password,
            int expiry,
            bool skipUpdateAcl = false
        )
        {
            TimeSpan? duration = null;
            if (expiry > 0)
                duration = TimeSpan.FromDays(expiry);

            var mqttUserNameHashKey = CacheKey.MQTT_USERNAME_HASH_KEY.GetCacheKey(username);
            var mqttUserNameHashField = CacheKey.MQTT_USERNAME_FIELD_KEY;

            if (duration != null)
            {
                await _cache.SetHashByKeyAsync(mqttUserNameHashKey, mqttUserNameHashField, password, duration.Value);
            }
            else
            {
                await _cache.SetHashByKeyAsync(mqttUserNameHashKey, mqttUserNameHashField, password);
            }

            if (!skipUpdateAcl)
            {
                // Add credentials for authorization
                var mqttUserNameAclHashKey = CacheKey.MQTT_USERNAME_ACL_HASH_KEY.GetCacheKey(username);

                await _cache.DeleteAsync(mqttUserNameAclHashKey);

                foreach (string topic in topics)
                {
                    if (duration != null)
                    {
                        await _cache.SetHashByKeyAsync(mqttUserNameAclHashKey, CacheKey.MQTT_USERNAME_ACL_FIELD_KEY.GetCacheKey(topic), "all", duration.Value);
                    }
                    else
                    {
                        await _cache.SetHashByKeyAsync(mqttUserNameAclHashKey, CacheKey.MQTT_USERNAME_ACL_FIELD_KEY.GetCacheKey(topic), "all");
                    }
                }
            }
        }

        private void SafetyRemove(IDictionary<string, object> source, string sourceKey)
        {
            if (source.ContainsKey(sourceKey))
            {
                source.Remove(sourceKey);
            }
        }

        public async Task<UpdateDeviceDto> UpdateAsync(UpdateDevice command, CancellationToken token)
        {
            await _deviceUnitOfWork.BeginTransactionAsync();
            var requestDeviceEntity = UpdateDevice.Create(command, _tenantContext.ProjectId);

            var oldDeviceEntity = await _deviceUnitOfWork.Devices.AsQueryable()
                                                               .AsNoTracking()
                                                               .Include(x => x.DeviceSnaphot)
                                                               .FirstOrDefaultAsync(x => x.Id == command.Id);

            try
            {
                await ValidateUpdateDeviceRequestAsync(oldDeviceEntity, command);

                if (oldDeviceEntity.DeviceContent != null)
                {
                    var payload = JsonConvert.DeserializeObject<BrokerIotDeviceDto>(oldDeviceEntity.DeviceContent);
                    payload["sasTokenDuration"] = command.SasTokenDuration.ToString();
                    requestDeviceEntity.DeviceContent = JsonConvert.SerializeObject(payload);
                }

                UpdateDeviceStatus(requestDeviceEntity, oldDeviceEntity);

                if (command.DeviceIdChanged)
                {
                    // Add new device
                    var newDeviceEntity = oldDeviceEntity.CloneAndUpdate(requestDeviceEntity);
                    newDeviceEntity.Id = command.UpdatedDeviceId;

                    await UpdateDeviceIdInIotAsync(newDeviceEntity, command.Id);
                    newDeviceEntity = await _deviceUnitOfWork.Devices.AddAsync(newDeviceEntity);
                    await _deviceUnitOfWork.Devices.UpdateDeviceRelationNavigationsAsync(command.Id, newDeviceEntity);

                    // Backup old device
                    oldDeviceEntity.Id = command.BackupDeviceId;
                    oldDeviceEntity.Deleted = true;
                    oldDeviceEntity.Template = null;
                    oldDeviceEntity.DeviceSnaphot = null;

                    await _deviceUnitOfWork.Devices.AddAsync(oldDeviceEntity);
                    await _deviceUnitOfWork.Devices.RemoveAsync(command.Id);
                    await TryUpdateEmqxDeviceAsync(oldDeviceEntity);
                }
                else
                {
                    await _deviceUnitOfWork.Devices.UpdateAsync(command.Id, requestDeviceEntity);
                    await TryUpdateEmqxDeviceAsync(requestDeviceEntity);
                }

                var tagIds = Array.Empty<long>();
                command.Upn = _userContext.Upn;
                command.ApplicationId = Guid.Parse(_userContext.ApplicationId ?? ApplicationInformation.APPLICATION_ID);
                if (command.Tags != null && command.Tags.Any())
                {
                    tagIds = await _tagService.UpsertTagsAsync(command);
                }

                await _deviceUnitOfWork.EntityTags.RemoveByEntityIdAsync(Privileges.Device.ENTITY_NAME, command.Id);

                if (tagIds.Any())
                {
                    var entitiesTags = tagIds.Distinct().Select(x => new EntityTagDb
                    {
                        EntityType = Privileges.Device.ENTITY_NAME,
                        EntityIdString = command.Id,
                        TagId = x
                    }).ToArray();
                    await _deviceUnitOfWork.EntityTags.AddRangeWithSaveChangeAsync(entitiesTags);
                }

                await _deviceUnitOfWork.CommitAsync();
            }
            catch (EntityValidationException ex)
            {
                await _deviceUnitOfWork.RollbackAsync();
                var customException = CreateCustomException(ex, nameof(command.Id), command.UpdatedDeviceId);
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActionType.Update, exception: customException, ActionStatus.Fail, command.Id, payload: new { payload = command, message = ex.DetailCode });
                throw;
            }
            catch (BaseException ex)
            {
                await _deviceUnitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActionType.Update, ActionStatus.Fail, command.Id, command.Name, payload: new { payload = command, message = ex.DetailCode });
                throw;
            }

            await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActionType.Update, ActionStatus.Success, requestDeviceEntity.Id, requestDeviceEntity.Name, payload: command);
            if (requestDeviceEntity.Status != oldDeviceEntity.Status)
            {
                var deviceConnectionChanged = new DeviceConnectionChangedEvent(requestDeviceEntity.Id, requestDeviceEntity.Status, _tenantContext, AHI.Infrastructure.Bus.ServiceBus.Enum.ActionTypeEnum.Updated);
                await _dispatcher.SendAsync(deviceConnectionChanged);
            }
            // Delete the redis cache.
            await _deviceBackgroundService.QueueAsync(_tenantContext, new CleanDeviceCache(new[] { command.Id }));


            await HandleRedisCacheWhenEntityChangedAsync(command.Id);

            // Clean topic cache.
            var changedTopics = new List<string>();
            if (string.Compare(oldDeviceEntity.TelemetryTopic, requestDeviceEntity.TelemetryTopic, StringComparison.CurrentCultureIgnoreCase) != 0)
            {
                if (!string.IsNullOrEmpty(oldDeviceEntity.TelemetryTopic))
                    changedTopics.Add(oldDeviceEntity.TelemetryTopic);
                if (!string.IsNullOrEmpty(requestDeviceEntity.TelemetryTopic))
                    changedTopics.Add(requestDeviceEntity.TelemetryTopic);
            }

            if (changedTopics.Any())
                await _deviceBackgroundService.QueueAsync(_tenantContext, new CleanTopicCache(changedTopics.ToArray()));

            if (command.DeviceIdChanged)
            {
                var deviceIdChangedEvent = new DeviceIdChangedEvent(command.Id, command.UpdatedDeviceId, command.Name, _tenantContext, AHI.Infrastructure.Bus.ServiceBus.Enum.ActionTypeEnum.Updated);
                await _dispatcher.SendAsync(deviceIdChangedEvent);

                // Update id in response
                requestDeviceEntity.Id = command.UpdatedDeviceId;
            }

            return await _tagService.FetchTagsAsync(UpdateDeviceDto.Create(requestDeviceEntity));
        }

        private async Task ValidateUpdateDeviceRequestAsync(Domain.Entity.Device oldDeviceEntity, UpdateDevice command)
        {
            if (oldDeviceEntity == null)
            {
                throw new EntityNotFoundException();
            }

            if (await IsTemplateDeleteAsync(oldDeviceEntity.TemplateId))
            {
                throw EntityValidationExceptionHelper.GenerateException(nameof(Domain.Entity.Device.TemplateId), MessageConstants.ERROR_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED, detailCode: MessageConstants.DETAIL_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED);
            }

            if (command.DeviceIdChanged || command.RetentionDays != oldDeviceEntity.RetentionDays)
            {
                if (oldDeviceEntity.DeviceSnaphot.Timestamp != null || oldDeviceEntity.DeviceSnaphot.CommandDataTimestamp != null)
                    throw EntityValidationExceptionHelper.GenerateException(nameof(Domain.Entity.Device.Id), MessageConstants.DETAIL_CODE_DEVICE_CANNOT_UPDATE_ID_AND_RETENTIONSDAYS, detailCode: MessageConstants.DETAIL_CODE_DEVICE_CANNOT_UPDATE_ID_AND_RETENTIONSDAYS);

                if (command.DeviceIdChanged)
                {
                    var hasExistDeviceId = await _deviceUnitOfWork.Devices.AsQueryable().AnyAsync(x => x.Id == command.UpdatedDeviceId);
                    if (hasExistDeviceId)
                        throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(command.UpdatedDeviceId));
                }
            }
        }

        private void UpdateDeviceStatus(Domain.Entity.Device requestDeviceEntity, Domain.Entity.Device oldDeviceEntity)
        {
            if (requestDeviceEntity.EnableHealthCheck)
            {
                if (oldDeviceEntity.EnableHealthCheck)
                {
                    // nothing change
                    requestDeviceEntity.Status = oldDeviceEntity.Status;
                }
                else
                {
                    // NA -> Disconnected
                    requestDeviceEntity.Status = CalculateDeviceStatus(requestDeviceEntity, oldDeviceEntity);
                }
                requestDeviceEntity.SignalQualityCode = oldDeviceEntity.SignalQualityCode;
            }
            else
            {
                // NA
                requestDeviceEntity.Status = SignalStatusConstants.NOT_APPLICABLE;
                requestDeviceEntity.SignalQualityCode = null;
            }
        }

        private string CalculateDeviceStatus(Domain.Entity.Device requestDeviceEntity, Domain.Entity.Device oldDeviceEntity)
        {
            if (!oldDeviceEntity.DeviceSnaphot.Timestamp.HasValue)
                return SignalStatusConstants.DISCONNECTED;

            bool disconnected = oldDeviceEntity.DeviceSnaphot.Timestamp.Value.AddSeconds(requestDeviceEntity.MonitoringTime ?? 0) < DateTime.UtcNow;
            return disconnected ? SignalStatusConstants.DISCONNECTED : SignalStatusConstants.CONNECTED;
        }

        private async Task TryUpdateEmqxDeviceAsync(Domain.Entity.Device device, bool triggerListener = true)
        {
            var projectId = string.Empty;
            try
            {
                if (device.DeviceContent == null)
                {
                    if (await _deviceUnitOfWork.DeviceTemplates.HasBindingAsync(device.TemplateId))
                    {
                        throw ValidationExceptionHelper.GenerateRequiredValidation(nameof(BrokerIotDeviceDto.BrokerId));
                    }

                    return;
                }

                var payload = JsonConvert.DeserializeObject<BrokerIotDeviceDto>(device.DeviceContent);
                if (!payload.BrokerId.HasValue)
                    return;

                projectId = payload.ProjectId;
                var broker = await GetBrokerByIdAsync(payload.BrokerId.Value, payload.ProjectId);
                var brokerContent = JsonConvert.DeserializeObject<BrokerContentDto>(broker.Content);
                if (broker.Status != BrokerStatus.ACTIVE)
                {
                    throw EntityValidationExceptionHelper.GenerateException(nameof(BrokerDto.Status), MessageConstants.ERROR_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED, detailCode: MessageConstants.DETAIL_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED);
                }

                // need to validate the sharing of broker
                // https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/2810
                if (projectId != _tenantContext.ProjectId && brokerContent.EnableSharing == false)
                {
                    throw EntityValidationExceptionHelper.GenerateException(nameof(BrokerIotDeviceDto.BrokerId), MessageConstants.ERROR_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED, detailCode: MessageConstants.DETAIL_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED);
                }

                if (BrokerConstants.EMQX_BROKERS.Contains(broker.Type))
                {
                    var topics = new List<string> { DEFAULT_TOPIC };
                    if (!string.IsNullOrEmpty(device.TelemetryTopic))
                        topics.Add(device.TelemetryTopic);
                    if (device.HasCommand.HasValue && device.HasCommand.Value! && !string.IsNullOrEmpty(device.CommandTopic))
                        topics.Add(device.CommandTopic);
                    topics = topics.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

                    var res = int.TryParse(payload["tokenDuration"].ToString(), out var expiry);
                    await UpdateBrokerInfoAsync(topics, broker.Id.ToString(), payload["username"].ToString(), payload["password"].ToString());
                    await UpdateEmqxRedisCredentialAsync(topics, payload["username"].ToString(), payload["password"].ToString(), expiry);

                    device.DeviceContent = JsonConvert.SerializeObject(payload);
                    if (triggerListener)
                        await _dispatcher.SendAsync(new BrokerChangedEvent(broker.Id, _tenantContext, true, AHI.Infrastructure.Bus.ServiceBus.Enum.ActionTypeEnum.Created));
                }
            }
            catch (System.Exception e)
            {
                _logger.LogError("TryUpdateEmqxDeviceAsync Failure: deviceId={deviceId},projectId={projectId}", device.Id, projectId);
            }
        }

        private async Task UpdateDeviceIdInIotAsync(Domain.Entity.Device newDevice, string oldDeviceId)
        {
            if (string.IsNullOrEmpty(newDevice.DeviceContent))
            {
                return;
            }

            var payload = JsonConvert.DeserializeObject<BrokerIotDeviceDto>(newDevice.DeviceContent);
            SafetyRemove(payload, DeviceContentPropertyConstants.ID);
            SafetyRemove(payload, DeviceContentPropertyConstants.NAME);
            SafetyRemove(payload, DeviceContentPropertyConstants.TEMPLATE_ID);
            SafetyRemove(payload, DeviceContentPropertyConstants.TEMPLATE_NAME);
            SafetyRemove(payload, DeviceContentPropertyConstants.STATUS);
            SafetyRemove(payload, DeviceContentPropertyConstants.BROKER);
            SafetyRemove(payload, DeviceContentPropertyConstants.CREATED_UTC);
            SafetyRemove(payload, DeviceContentPropertyConstants.UPDATED_UTC);
            SafetyRemove(payload, DeviceContentPropertyConstants.DESCRIPTION);
            SafetyRemove(payload, DeviceContentPropertyConstants.DEVICE_TEMPLATES);

            if (!payload.BrokerId.HasValue)
            {
                return;
            }

            var broker = await GetBrokerByIdAsync(payload.BrokerId.Value, payload.ProjectId);
            var brokerFunction = _httpClientFactory.CreateClient(HttpClientNames.BROKER_FUNCTION, _tenantContext);

            if (broker.IsEmqxDevice)
            {
                return;
            }

            // Unregister the old iot device
            payload[DeviceContentPropertyConstants.DEVICE_ID] = oldDeviceId;
            var requestBody = new Dictionary<string, object>(payload)
                                {
                                    { "tenantId", _tenantContext.TenantId.ToString() },
                                    { "subscriptionId", _tenantContext.SubscriptionId.ToString() },
                                    { "brokerContent", broker.Content }
                                };
            var response = await brokerFunction.PostAsync("fnc/bkr/device/unregister", new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            // Register the new iot device
            payload[DeviceContentPropertyConstants.DEVICE_ID] = newDevice.Id;
            await ProccessIotHubBrokerAsync(newDevice, broker, broker.DeserializedContent, payload);

            newDevice.DeviceContent = JsonConvert.SerializeObject(payload);
        }

        private string GenerateConnectionString(string brokerContent, string deviceId, string primaryKey)
        {
            var contentJsonParse = JObject.Parse(brokerContent);
            var iotHubName = contentJsonParse["iot_hub_name"].ToString();
            return $"HostName={iotHubName}.azure-devices.net;DeviceId={deviceId};SharedAccessKey={primaryKey}";
        }

        private string GetResoureEndpoint(string brokerContent, string deviceId)
        {
            var contentJsonParse = JObject.Parse(brokerContent);
            var iotHubName = contentJsonParse["iot_hub_name"].ToString();
            return $"https://{iotHubName}.azure-devices.net/devices/{deviceId}";
        }

        private string GenerateSASToken(string resourceUri, string key, int duration, string keyName = "iothubowner")
        {
            resourceUri = resourceUri.TrimStart("https://".ToCharArray());
            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var timeExpired = TimeSpan.FromDays(duration);
            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + timeExpired.TotalSeconds);
            string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
            HMACSHA256 hmac = new HMACSHA256(Convert.FromBase64String(key));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry);
            if (!String.IsNullOrEmpty(keyName))
                sasToken += "&skn=" + keyName;
            return sasToken;
        }

        public async Task<BaseResponse> PushConfigurationMessageAsync(PushMessageToDevice command, CancellationToken token)
        {
            var metricPayloads = string.Empty;
            var isValidationError = false;
            try
            {
                var iotDevice = await FindByIdAsync(new GetDeviceById(command.Id), token);
                (
                    BrokerIotDeviceDto payload,
                    BrokerDto broker,
                    Dictionary<string, object> dictionaryBindings,
                    IEnumerable<CloudToDeviceMessage> metrics
                ) = await ValidatePushConfigurationMessageAsync(iotDevice, command, (isError) => isValidationError = isError);

                foreach (var metric in metrics)
                {
                    ProcessCloudToDeviceMetrics(metric, metrics, iotDevice, dictionaryBindings);
                }

                var messageConfiguration = JsonConvert.SerializeObject(dictionaryBindings);
                metricPayloads = messageConfiguration;

                var client = _httpClientFactory.CreateClient(HttpClientNames.BROKER_FUNCTION);
                var response = await client.PostAsync($"fnc/bkr/device/push", new StringContent(JsonConvert.SerializeObject(new Dictionary<string, object>(payload)
                {
                    {"tenantId",_tenantContext.TenantId.ToString()},
                    {"subscriptionId",_tenantContext.SubscriptionId.ToString()},
                    {"brokerContent",broker.Content},
                    {"messageContent", messageConfiguration},
                    {"deviceContent", iotDevice.DeviceContent}
                }), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActivitiesLogEventAction.Send_Data, ActionStatus.Success, command.Id, payload: metricPayloads);
                    return BaseResponse.Success;
                }
                else
                {
                    await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActivitiesLogEventAction.Send_Data, ActionStatus.Fail, command.Id, payload: new { payload = metricPayloads, message = MessageConstants.DEVICE_SEND_CONFIGURATION_FAIL });
                    return BaseResponse.Failed;
                }
            }
            catch (System.Exception e)
            {
                if (!isValidationError)
                    await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActivitiesLogEventAction.Send_Data, ActionStatus.Fail, command.Id, payload: new { payload = command, message = e.Message });
                throw;
            }
        }

        private async Task<(
            BrokerIotDeviceDto payload,
            BrokerDto broker,
            Dictionary<string, object> dictionaryBindings,
            IEnumerable<CloudToDeviceMessage> metrics
        )> ValidatePushConfigurationMessageAsync(GetDeviceDto iotDevice, PushMessageToDevice command, Action<bool> SetValidationError)
        {
            if (iotDevice == null)
            {
                throw new EntityNotFoundException();
            }
            if (iotDevice.DeviceContent == null)
            {
                throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_IOT_HUB_NOT_VALID);
            }
            var payload = JsonConvert.DeserializeObject<BrokerIotDeviceDto>(iotDevice.DeviceContent);
            if (payload.BrokerId == null)
            {
                throw new EntityInvalidException(message: MessageConstants.DEVICE_BROKER_NOT_SUPPORTED, detailCode: MessageConstants.DEVICE_BROKER_NOT_SUPPORTED);
            }
            if (!command.Metrics.Any())
            {
                throw ValidationExceptionHelper.GenerateRequiredValidation(nameof(PushMessageToDevice.Metrics));
            }
            var broker = await GetBrokerByIdAsync(payload.BrokerId.Value, payload.ProjectId ?? _tenantContext.ProjectId);
            var dictionaryBindings = new Dictionary<string, object>();
            var metrics = command.Metrics;

            //validate for emqx broker type
            if (BrokerConstants.EMQX_BROKERS.Contains(broker.Type))
            {
                if (iotDevice.HasCommand == false)
                {
                    throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_OPTION_SEND_IS_DISABLE);
                }
                broker.Content = ReMapEMQXTopic(broker.Content, iotDevice);
            }

            // validation metric binding
            var validationErrors = ValidateMetric(metrics);
            if (validationErrors.Any())
            {
                SetValidationError(true);
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActivitiesLogEventAction.Send_Data, ActionStatus.Fail, command.Id, payload: validationErrors);
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(PushMessageToDevice.Metrics));
            }

            return (payload, broker, dictionaryBindings, metrics);
        }

        private void ProcessCloudToDeviceMetrics(CloudToDeviceMessage metric, IEnumerable<CloudToDeviceMessage> metrics, GetDeviceDto iotDevice, Dictionary<string, object> dictionaryBindings)
        {
            if (string.IsNullOrWhiteSpace(metric.Value))
                return;

            var templateBinding = iotDevice.Template.Bindings.FirstOrDefault(x => x.Key == metric.Key);

            // check template binding exists
            if (templateBinding == null)
            {
                switch (metrics.Count())
                {
                    case 1: // send only 1 metric
                        throw new EntityNotFoundException(message: MessageDetailConstants.METRIC_DELETED); // message: This item has been deleted.
                    default: // send all metric
                        throw new EntityNotFoundException(message: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED, detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED); // message: Some selected item(s) have been deleted.
                }
            }
            dictionaryBindings[metric.Key] = StringExtension.ParseValue(metric.Value, templateBinding.DataType, int.MaxValue);

            if (templateBinding.DataType == DataTypeConstants.TYPE_DOUBLE)
            {
                var position = metric.Value.IndexOf(".");
                metric.Value = (position > 0 && (metric.Value.Length - position) >= 18) ? metric.Value.Substring(0, position + 18) : metric.Value;
            }
        }

        public async Task<BaseResponse> PushConfigurationMessageMutipleAsync(IEnumerable<PushMessageToDevice> commands, CancellationToken token)
        {
            var metricPayloads = string.Empty;
            var isValidationError = false;
            try
            {
                var lstBrokerIotDeviceDto = new List<BrokerIotDeviceDto>();
                var payloadBindings = new List<Dictionary<string, string>>();

                foreach (var item in commands)
                {
                    var dictionaryBindings = new List<Dictionary<string, object>>();
                    var iotDevice = await ValidateIotDeviceAsync(item, isValidationError, dictionaryBindings, token);
                    var payload = iotDevice.DeviceContent.FromJson<BrokerIotDeviceDto>();

                    var broker = await GetBrokerByIdAsync(payload.BrokerId.Value, payload.ProjectId ?? _tenantContext.ProjectId);
                    metricPayloads = dictionaryBindings.ToJson();

                    //validate for emqx broker type
                    if (BrokerConstants.EMQX_BROKERS.Contains(broker.Type))
                    {
                        if (iotDevice.HasCommand == false)
                        {
                            throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_OPTION_SEND_IS_DISABLE);
                        }
                        broker.Content = ReMapEMQXTopic(broker.Content, iotDevice);
                    }

                    var configurationMessage = new Dictionary<string, object>(payload)
                    {
                    { "tenantId",_tenantContext.TenantId.ToString()},
                    { "subscriptionId",_tenantContext.SubscriptionId.ToString()},
                    { "brokerContent",broker.Content},
                    { "messageContent", metricPayloads},
                    { "deviceContent", iotDevice.DeviceContent}
                    };

                    // send config
                    var client = _httpClientFactory.CreateClient(HttpClientNames.BROKER_FUNCTION);
                    var response = await client.PostAsync($"fnc/bkr/device/push", new StringContent(configurationMessage.ToJson(), Encoding.UTF8, "application/json"));

                    if (!response.IsSuccessStatusCode)
                    {
                        await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActivitiesLogEventAction.Send_Data, ActionStatus.Fail, string.Join(",", commands.Select(x => x.Id)), payload: new { payload = metricPayloads, message = MessageConstants.DEVICE_SEND_CONFIGURATION_FAIL });
                        return BaseResponse.Failed;
                    }
                }
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActivitiesLogEventAction.Send_Data, ActionStatus.Success, string.Join(",", commands.Select(x => x.Id)), payload: metricPayloads);
                return BaseResponse.Success;
            }
            catch (System.Exception e)
            {
                if (!isValidationError)
                    await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActivitiesLogEventAction.Send_Data, ActionStatus.Fail, string.Join(",", commands.Select(x => x.Id)), payload: new { payload = commands, message = e.Message });
                throw;
            }
        }

        private string ReMapEMQXTopic(string brokerContent, GetDeviceDto iotDevice)
        {
            var brokerContentDict = brokerContent.FromJson<Dictionary<string, object>>();
            brokerContentDict[BrokerContentKeys.COMMAND_TOPIC] = iotDevice.CommandTopic;
            brokerContentDict[BrokerContentKeys.TELEMETRY_TOPIC] = iotDevice.TelemetryTopic;
            return brokerContentDict.ToJson();
        }

        private async Task<GetDeviceDto> ValidateIotDeviceAsync(PushMessageToDevice cloudToDevice, bool isValidationError, List<Dictionary<string, object>> dictionaryBindings, CancellationToken token)
        {
            var iotDevice = await FindByIdAsync(new GetDeviceById(cloudToDevice.Id), token);
            if (iotDevice == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation("DeviceId");

            if (iotDevice.DeviceContent == null)
            {
                throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_IOT_HUB_NOT_VALID);
            }
            var payload = iotDevice.DeviceContent.FromJson<BrokerIotDeviceDto>();
            if (payload.BrokerId == null)
            {
                throw new EntityInvalidException(message: MessageConstants.DEVICE_BROKER_NOT_SUPPORTED, detailCode: MessageConstants.DEVICE_BROKER_NOT_SUPPORTED);
            }
            if (!cloudToDevice.Metrics.Any())
            {
                throw ValidationExceptionHelper.GenerateRequiredValidation(nameof(PushMessageToDevice.Metrics));
            }

            // validate MetricKey and update dataType for MetricKey
            foreach (var metric in cloudToDevice.Metrics)
            {
                var bindingTemplate = iotDevice.Template.Bindings.FirstOrDefault(x => x.Key == metric.Key);
                if (bindingTemplate == null)
                    throw ValidationExceptionHelper.GenerateNotFoundValidation("MetricKey");
                metric.DataType = bindingTemplate.DataType;
            }

            await PrepareDeviceMessage(iotDevice, cloudToDevice.Metrics, isValidationError, dictionaryBindings);

            return iotDevice;
        }
        private async Task PrepareDeviceMessage(GetDeviceDto iotDevice, IEnumerable<CloudToDeviceMessage> metrics, bool isValidationError, List<Dictionary<string, object>> dictionaryBindings)
        {

            // validation metric binding
            var validationErrors = ValidateMetric(metrics);
            if (validationErrors.Any())
            {
                isValidationError = true;
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActivitiesLogEventAction.Send_Data, ActionStatus.Fail, iotDevice.Id, payload: validationErrors);
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(PushMessageToDevice.Metrics));
            }
            foreach (var metric in metrics)
            {
                if (string.IsNullOrWhiteSpace(metric.Value))
                    continue;

                var templateBinding = iotDevice.Template.Bindings.FirstOrDefault(x => x.Key == metric.Key);

                // check template binding exists
                if (templateBinding == null)
                {
                    switch (metrics.Count())
                    {
                        case 1: // send only 1 metric
                            throw new EntityNotFoundException(message: MessageDetailConstants.METRIC_DELETED); // message: This item has been deleted.
                        default: // send all metric
                            throw new EntityNotFoundException(message: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED, detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED); // message: Some selected item(s) have been deleted.
                    }
                }

                var dictionaryBinding = new Dictionary<string, object>();
                dictionaryBinding[metric.Key] = StringExtension.ParseValue(metric.Value, templateBinding.DataType, int.MaxValue);
                dictionaryBindings.Add(dictionaryBinding);

                if (templateBinding.DataType == DataTypeConstants.TYPE_DOUBLE)
                {
                    var position = metric.Value.IndexOf(".");
                    metric.Value = (position > 0 && (metric.Value.Length - position) >= 18) ? metric.Value.Substring(0, position + 18) : metric.Value;
                }
            }
        }
        private IEnumerable<ContentLogError> ValidateMetric(IEnumerable<CloudToDeviceMessage> bindings)
        {
            var response = new List<ContentLogError>();
            foreach (var metric in bindings)
            {
                var content = new ContentLogError();
                content.Metric = metric.Key;
                content.DataType = metric.DataType;
                content.Value = metric.Value;
                var isValid = true;

                if (string.IsNullOrWhiteSpace(metric.Value))
                    continue;

                try
                {
                    ParseCloudToDeviceMetricValue(metric);
                }
                catch (OverflowException)
                {
                    isValid = false;
                    content.Message = MessageDetailConstants.OUT_OF_RANGE_OF_DATA_TYPE;
                }
                catch (System.Exception)
                {
                    isValid = false;
                    content.Message = MessageDetailConstants.INVALID_VALUE;
                }
                if (!isValid)
                    response.Add(content);
            }
            return response;
        }

        private void ParseCloudToDeviceMetricValue(CloudToDeviceMessage metric)
        {
            switch (metric.DataType)
            {
                case DataTypeConstants.TYPE_BOOLEAN:
                    bool.Parse(metric.Value);
                    break;

                case DataTypeConstants.TYPE_DOUBLE:
                    var value = double.Parse(metric.Value);
                    if (double.IsInfinity(value))
                        throw new OverflowException();
                    break;

                case DataTypeConstants.TYPE_INTEGER:
                    int.Parse(metric.Value);
                    break;

                case DataTypeConstants.TYPE_TEXT:
                    if (!Regex.IsMatch(metric.Value, "^(?=.*$)"))
                        throw new System.Exception();
                    break;

                default:
                    throw new System.Exception();
            }
        }

        private bool IsValidKey(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out int bytesParsed) && bytesParsed >= 16 && bytesParsed <= 64;
        }

        public async Task<UpdateDeviceDto> PartialUpdateAsync(PatchDevice payload, CancellationToken token)
        {
            await _deviceUnitOfWork.BeginTransactionAsync();
            var deviceDB = await _deviceUnitOfWork.Devices.FindAsync(payload.Id);
            if (deviceDB == null)
            {
                throw new EntityNotFoundException();
            }
            try
            {
                foreach (Operation operation in payload.JsonPatch.Operations)
                {
                    switch (operation.op)
                    {
                        case "update/device/primary_key":
                            await RegeneratePrimaryKeyAsync(deviceDB, operation.value);
                            break;

                        case "update/device/sas_token":
                            await RegenerateSaSTokenAsync(deviceDB, operation.value);
                            break;
                    }
                }
                await _deviceUnitOfWork.CommitAsync();

                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActivitiesLogEventAction.Generate_Primary_Key, ActionStatus.Success, deviceDB.Id, deviceDB.Name, payload: payload);
            }
            catch
            {
                await _deviceUnitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActivitiesLogEventAction.Generate_Primary_Key, ActionStatus.Fail, deviceDB.Id, deviceDB.Name, payload: payload);
                throw;
            }
            await HandleRedisCacheWhenEntityChangedAsync(deviceDB.Id);
            return new UpdateDeviceDto()
            {
                Id = payload.Id
            };
        }

        private async Task RegenerateSaSTokenAsync(Domain.Entity.Device device, object value)
        {
            var payload = JsonConvert.DeserializeObject<BrokerIotDeviceDto>(device.DeviceContent);
            var sasTokeRequest = JObject.FromObject(value).ToObject<IoTDeviceTokenRequest>();
            if (payload.BrokerId.HasValue)
            {
                var projectId = payload.ProjectId;
                var broker = await GetBrokerByIdAsync(payload.BrokerId.Value, payload.ProjectId);
                var brokerContent = JsonConvert.DeserializeObject<BrokerContentDto>(broker.Content);
                var resourceEndpoint = GetResoureEndpoint(broker.Content, device.Id);
                var primaryKey = payload["primaryKey"].ToString();
                payload["sasToken"] = GenerateSASToken(resourceEndpoint, primaryKey, sasTokeRequest.SasTokenDuration, null);
                device.DeviceContent = JsonConvert.SerializeObject(payload);
            }
        }

        private async Task RegeneratePrimaryKeyAsync(Domain.Entity.Device device, object value)
        {
            var payload = JsonConvert.DeserializeObject<BrokerIotDeviceDto>(device.DeviceContent);
            var sasTokeRequest = JObject.FromObject(value).ToObject<IoTDeviceTokenRequest>();
            if (payload.BrokerId.HasValue)
            {
                try
                {
                    var projectId = payload.ProjectId;
                    var broker = await GetBrokerByIdAsync(payload.BrokerId.Value, payload.ProjectId);
                    var brokerContent = JsonConvert.DeserializeObject<BrokerContentDto>(broker.Content);
                    // need to validate the sharing of broker
                    // https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/2810
                    if (projectId != _tenantContext.ProjectId && brokerContent.EnableSharing == false)
                    {
                        throw new EntityNotFoundException(detailCode: MessageConstants.DEVICE_IOT_HUB_NOT_FOUND);
                    }
                    var primaryKey = sasTokeRequest.PrimaryKey;
                    payload["primaryKey"] = sasTokeRequest.PrimaryKey;
                    var brokerFunction = _httpClientFactory.CreateClient(HttpClientNames.BROKER_FUNCTION);
                    var requestBody = new Dictionary<string, object>(payload)
                    {
                        {"tenantId",_tenantContext.TenantId.ToString()},
                        {"subscriptionId",_tenantContext.SubscriptionId.ToString()},
                        {"brokerContent",broker.Content},
                        {"secondaryKey",sasTokeRequest.SecondaryKey}
                    };
                    if (!IsValidKey(sasTokeRequest.PrimaryKey))
                    {
                        throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_PRIMARY_KEY_INVALID);
                    }
                    var tokenResponse = await brokerFunction.PostAsync("fnc/bkr/device/key/regenerate", new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));
                    tokenResponse.EnsureSuccessStatusCode();
                    payload["connectionString"] = GenerateConnectionString(broker.Content, device.Id, primaryKey);
                    var duration = Convert.ToInt32(payload.ContainsKey("sasTokenDuration") ? payload["sasTokenDuration"] : "30");
                    var resourceEndpoint = GetResoureEndpoint(broker.Content, device.Id);
                    payload["sasToken"] = GenerateSASToken(resourceEndpoint, primaryKey, duration, null);
                    device.DeviceContent = JsonConvert.SerializeObject(payload);
                }
                catch
                {
                    throw new EntityInvalidException(nameof(IoTDeviceTokenRequest.PrimaryKey), MessageConstants.DEVICE_GENERATE_PRIMARY_FAILED);
                }
            }
        }

        public async Task<GetDeviceDto> FindByIdAsync(GetDeviceById payload, CancellationToken token)
        {
            Domain.Entity.Device entity = await _readDeviceRepository.FindAsync(payload.Id);
            if (entity == null)
                throw new EntityNotFoundException();
            GetDeviceDto result = GetDeviceDto.Create(entity);

            return await _tagService.FetchTagsAsync(result);
        }

        public async Task<BaseResponse> RemoveEntityForceAsync(DeleteDevice command, CancellationToken token)
        {
            await _deviceUnitOfWork.BeginTransactionAsync();
            var deviceNames = new List<string>();
            try
            {
                var deletedEntities = await _deviceUnitOfWork.Devices.AsQueryable()
                                                    .Where(entity => command.DeviceIds.Contains(entity.Id))
                                                    .Select(entity => new { entity.Id, entity.Name, entity.DeviceContent })
                                                    .ToListAsync();
                deviceNames.AddRange(deletedEntities.Select(x => x.Name));

                if (await CheckDeviceUsedByAssetAttributeAsync(command.DeviceIds))
                    throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_USING);

                if (command.DeviceIds.Count() != deletedEntities.Count)
                {
                    throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
                }
                var brokerFunction = _httpClientFactory.CreateClient(HttpClientNames.BROKER_FUNCTION, _tenantContext);
                foreach (var deleteEntity in deletedEntities)
                {
                    await UnregisterDeviceAsync(deleteEntity.DeviceContent, brokerFunction);
                    _ = await _deviceUnitOfWork.Devices.RemoveAsync(deleteEntity.Id);
                }
                await _deviceUnitOfWork.CommitAsync();
                await HandleRedisCacheWhenEntityChangedAsync(deletedEntities.Select(i => i.Id));
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error in DeviceService - RemoveEntityForceAsync: {ex.Message}");
                await _deviceUnitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActionType.Delete, ActionStatus.Fail, command.DeviceIds, deviceNames, payload: command);
                throw;
            }

            await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActionType.Delete, ActionStatus.Success, command.DeviceIds, deviceNames, payload: command);
            int totalDevice = await _deviceUnitOfWork.Devices.GetTotalDeviceAsync();
            var deviceAdded = new DeviceChangedEvent(string.Join(',', command.DeviceIds), totalDevice, false, _tenantContext);
            await _dispatcher.SendAsync(deviceAdded);

            // Delete the redis cache.
            await _deviceBackgroundService.QueueAsync(_tenantContext, new CleanDeviceCache(command.DeviceIds));

            return BaseResponse.Success;
        }

        private async Task UnregisterDeviceAsync(string deviceContent, HttpClient brokerFunction)
        {
            if (deviceContent == null)
                return;
            var payload = JsonConvert.DeserializeObject<BrokerIotDeviceDto>(deviceContent);
            if (!payload.BrokerId.HasValue)
                return;

            try
            {
                var broker = await GetBrokerByIdAsync(payload.BrokerId.Value, payload.ProjectId);

                if (BrokerConstants.EMQX_BROKERS.Contains(broker.Type))
                {
                    var requestBody = new Dictionary<string, object>(payload)
                        {
                            {"clientId", payload.Username}
                        };
                    var tokenResponse = await brokerFunction.PostAsync("fnc/bkr/emqx/remove/client", new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));
                    tokenResponse.EnsureSuccessStatusCode();
                }
                else
                {
                    var requestBody = new Dictionary<string, object>(payload)
                        {
                            {"tenantId", _tenantContext.TenantId.ToString()},
                            {"subscriptionId", _tenantContext.SubscriptionId.ToString()},
                            {"brokerContent", broker.Content}
                        };
                    var tokenResponse = await brokerFunction.PostAsync("fnc/bkr/device/unregister", new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));
                    tokenResponse.EnsureSuccessStatusCode();
                }
            }
            catch (EntityNotFoundException ex)
            {
                // still delete device when broker deleted
                _logger.LogError(ex, $"Error in DeviceService - UnregisterDeviceAsync: {ex.Message}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error in DeviceService - UnregisterDeviceAsync: {ex.Message}");
                throw;
            }
        }

        private async Task<bool> CheckDeviceUsedByAssetAttributeAsync(IEnumerable<string> deviceIds)
        {
            var attributeDynamics = await _readAssetAttributeRepository.AsQueryable().AsNoTracking()
                .Where(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC || x.AttributeType == AttributeTypeConstants.TYPE_COMMAND)
                .Include(x => x.AssetAttributeDynamic)
                .Include(x => x.AssetAttributeCommand)
                .ToListAsync();
            var attributeDynamicMappings = await _readAssetAttributeTemplateRepository.AsQueryable().AsNoTracking()
                .Where(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC || x.AttributeType == AttributeTypeConstants.TYPE_COMMAND)
                .Include(x => x.AssetAttributeDynamicMappings)
                .Include(x => x.AssetAttributeCommandMappings)
                .ToListAsync();
            foreach (var deviceId in deviceIds)
            {
                var deviceIdsUsing = new List<string>();
                if (attributeDynamics.Any())
                    ExtractDeviceIds(attributeDynamics, deviceIdsUsing);
                if (attributeDynamicMappings.Any())
                    ExtractDeviceIds(attributeDynamicMappings, deviceIdsUsing);
                if (deviceIdsUsing.Contains(deviceId))
                    return true;
            }
            return false;
        }

        private void ExtractDeviceIds(List<Domain.Entity.AssetAttribute> attributeDynamics, List<string> deviceIdsUsing)
        {
            foreach (var item in attributeDynamics)
            {
                if (item.AssetAttributeDynamic == null && item.AssetAttributeCommand == null)
                    continue;

                if (item.AssetAttributeDynamic != null)
                    deviceIdsUsing.Add(item.AssetAttributeDynamic.DeviceId);
                else
                    deviceIdsUsing.Add(item.AssetAttributeCommand.DeviceId);
            }
        }

        private void ExtractDeviceIds(List<Domain.Entity.AssetAttributeTemplate> attributeDynamicMappings, List<string> deviceIdsUsing)
        {
            foreach (var item in attributeDynamicMappings)
            {
                deviceIdsUsing.AddRange(item.AssetAttributeDynamicMappings.Select(x => x.DeviceId));
                deviceIdsUsing.AddRange(item.AssetAttributeCommandMappings.Select(x => x.DeviceId));
            }
        }

        public async Task<IEnumerable<GetMetricsByDeviceIdDto>> GetMetricsByDeviceIdAsync(GetMetricsByDeviceId request, CancellationToken cancellationToken)
        {
            var dataResponse = new List<GetMetricsByDeviceIdDto>();
            var device = await _readDeviceRepository.AsQueryable().AsNoTracking().Include(x => x.Template)
                                                        .ThenInclude(x => x.Payloads)
                                                        .ThenInclude(x => x.Details)
                                                        .ThenInclude(x => x.TemplateKeyType)
                                                        .FirstOrDefaultAsync(x => x.Id == request.DeviceId);
            if (device == null)
            {
                throw new EntityNotFoundException();
            }
            foreach (var payload in device.Template.Payloads)
            {
                //Bug https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/61089
                //get both enabled and disabled metrics
                var metrics = payload.Details
                    .Where(x => (x.TemplateKeyType.Name == TemplateKeyTypeConstants.METRIC || x.TemplateKeyType.Name == TemplateKeyTypeConstants.AGGREGATION));
                if (!request.IsIncludeDisabledMetric)
                {
                    metrics = metrics.Where(x => x.Enabled);
                }
                if (metrics != null && metrics.Any())
                    foreach (var metric in metrics)
                    {
                        dataResponse.Add(GetMetricsByDeviceIdDto.Create(metric));
                    }
            }
            return dataResponse;
        }

        public async Task<ActivityResponse> ExportAsync(ExportDevice request, CancellationToken cancellationToken)
        {
            try
            {
                await CheckExistDevicesAsync(new CheckExistDevice(request.Ids), cancellationToken);
                await _fileEventService.SendExportEventAsync(request.ActivityId, request.ObjectType, request.Ids);
                return new ActivityResponse(request.ActivityId);
            }
            catch
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.DEVICE, ActionType.Export, ActionStatus.Fail, payload: request);
                throw;
            }
        }

        private async Task<bool> IsTemplateDeleteAsync(Guid templateId)
        {
            var checkTemplateExists = await _readValidTemplateRepository.AsQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Id == templateId);
            return checkTemplateExists == null;
        }

        public async Task<IEnumerable<SnapshotDto>> GetMetricSnapshotAsync(GetMetricSnapshot request, CancellationToken cancellationToken)
        {
            var response = await _deviceUnitOfWork.Devices.GetMetricSnapshotAsync(request.DeviceId);
            return response.Select(SnapshotDto.Create);
        }

        public async Task<IEnumerable<GetDeviceDto>> GetDevicesByTemplateIdAsync(GetDevicesByTemplateId request, CancellationToken cancellationToken)
        {
            var response = await _deviceUnitOfWork.Devices.GetDevicesByTemplateIdAsync(request.Id);
            return response.Select(GetDeviceDto.Create);
        }

        public async Task<IEnumerable<SharingBrokerDto>> SearchSharingBrokerAsync(SearchSharingBroker command, CancellationToken cancellationToken)
        {
            var list = await _deviceUnitOfWork.Devices.AsQueryable().Where(x => x.DeviceContent.Contains("\"enable_sharing\":\"true\"")).ToListAsync();
            return list.Select(x => JsonConvert.DeserializeObject<BrokerIotDeviceDto>(x.DeviceContent)).Select(x => new SharingBrokerDto()
            {
                BrokerId = x.BrokerId.Value,
                ProjectId = x.ProjectId ?? _tenantContext.ProjectId
            });
        }

        public async Task<BaseResponse> CheckExistDevicesAsync(CheckExistDevice command, CancellationToken cancellationToken)
        {
            var requestIds = new HashSet<string>(command.Ids.Distinct());
            var deviceIds = await _readDeviceRepository.AsQueryable().AsNoTracking().Where(x => requestIds.Contains(x.Id)).Select(x => x.Id).ToListAsync();
            var devices = new HashSet<string>(deviceIds);
            if (!requestIds.SetEquals(devices))
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
            return BaseResponse.Success;
        }

        public async Task<bool> CheckExistMetricByDeviceIdAsync(string metricKey, string deviceId)
        {
            if (string.IsNullOrWhiteSpace(metricKey) || string.IsNullOrWhiteSpace(deviceId))
                return false;

            var metrics = await GetMetricsByDeviceIdAsync(new GetMetricsByDeviceId(deviceId, true), CancellationToken.None);
            return metrics.Any(x => x.MetricKey == metricKey);
        }

        public async Task<MetricAssemblyDto> GenerateMetricAssemblyAsync(string deviceId, CancellationToken cancellationToken)
        {
            string deviceName = null;
            var activityId = Guid.NewGuid();
            _notification.Upn = _userContext.Upn;
            _notification.ActivityId = activityId;
            _notification.ObjectType = ObjectType.METRIC;
            _notification.NotificationType = ActionType.Export;
            await _notification.SendStartNotifyAsync(1, isExport: false);
            try
            {
                var device = await _deviceUnitOfWork.Devices.FindAsync(deviceId);
                if (device == null)
                    throw new EntityNotFoundException();
                deviceName = device.Name;

                var deviceTemplate = await _deviceUnitOfWork.DeviceTemplates.FindAsync(device.TemplateId);
                if (deviceTemplate == null)
                    throw new EntityNotFoundException(detailCode: MessageConstants.DEVICE_TEMPLATE_NOT_FOUND);
                var metrics = new List<MetricInfomation>();
                var deviceKey = "device";
                var metricRow = new List<string>();
                var valueRow = new List<object>();
                foreach (var payload in deviceTemplate.Payloads)
                {
                    metrics.AddRange(payload.Details.Where(x => x.Enabled && x.KeyTypeId != (int)TemplateKeyTypeEnums.Aggregation)
                                                    .Select(x => new MetricInfomation()
                                                    {
                                                        Key = x.Key,
                                                        DataType = x.DataType,
                                                        KeyTypeId = x.KeyTypeId,
                                                        Value = GenerateSampleMetricValue(x)
                                                    }));
                }
                foreach (var metric in metrics)
                {
                    if (metric.KeyTypeId == (int)TemplateKeyTypeEnums.DeviceId)
                        deviceKey = metric.Key.Replace("\"", "\"\"");
                    else if (metric.KeyTypeId == (int)TemplateKeyTypeEnums.Timestamp)
                    {
                        metricRow.Insert(0, metric.Key.Replace("\"", "\"\""));
                        valueRow.Insert(0, metric.Value.ToString().Replace("\"", "\"\""));
                    }
                    else
                    {
                        metricRow.Add(metric.Key.Replace("\"", "\"\""));
                        valueRow.Add(metric.Value.ToString().Replace("\"", "\"\""));
                    }
                }
                var csvContent = $"\"{deviceKey}\",\"{deviceId.Replace("\"", "\"\"")}\"\n\"{String.Join("\",\"", metricRow)}\"\n\"{String.Join("\",\"", valueRow)}\"";
                await _notification.SendFinishExportNotifyAsync(ActionStatus.Success, isExport: false);
                await _auditLogService.SendLogAsync(activityId, ActivityEntityAction.DEVICE, ActivitiesLogEventAction.Download_Metrics, ActionStatus.Success, deviceId, deviceName);
                return new MetricAssemblyDto($"{deviceId}.csv", Encoding.UTF8.GetBytes(csvContent));
            }
            catch (System.Exception e)
            {
                await _notification.SendFinishExportNotifyAsync(ActionStatus.Fail, isExport: false);
                await _auditLogService.SendLogAsync(activityId, ActivityEntityAction.DEVICE, ActivitiesLogEventAction.Download_Metrics, e, deviceId, deviceName);
                throw;
            }
        }

        private object GenerateSampleMetricValue(Domain.Entity.TemplateDetail x)
        {
            var textSample = "text sample";
            return x.DataType == DataTypeConstants.TYPE_BOOLEAN ? (object)false :
                   x.DataType == DataTypeConstants.TYPE_INTEGER ? (object)1 :
                   x.DataType == DataTypeConstants.TYPE_DOUBLE ? (object)1.1 :
                   x.DataType == DataTypeConstants.TYPE_TIMESTAMP ? (object)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() : textSample;
        }

        public async Task<BaseSearchResponse<GetDeviceDto>> FindDeviceHasBinding(GetDeviceHasBinding payload, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            var devices = await _readDeviceRepository.AsQueryable().Include(x => x.Template).ThenInclude(x => x.Bindings).AsNoTracking().Where(x => x.Template.Bindings != null && x.Template.Bindings.Any() && x.Id.Contains(payload.DeviceId))
               .Skip(payload.PageIndex * payload.PageSize).Take(payload.PageSize)
               .ToListAsync();
            var result = devices.Select(GetDeviceDto.Create).OrderBy(x => x.CreatedUtc);
            var totalMilliseconds = (long)(DateTime.UtcNow - start).TotalMilliseconds;
            return new BaseSearchResponse<GetDeviceDto>(totalMilliseconds, result.Count(), payload.PageSize, payload.PageIndex, result);
        }

        public async Task<IEnumerable<ArchiveDeviceDto>> ArchiveAsync(ArchiveDevice command, CancellationToken cancellationToken)
        {
            var devices = await _readDeviceRepository.AsQueryable().AsNoTracking()
                                            .Include(x => x.EntityTags)
                                            .Where(a => !a.Deleted && a.UpdatedUtc <= command.ArchiveTime && (!a.EntityTags.Any() || a.EntityTags.Any(e => e.EntityType == Privileges.Device.ENTITY_NAME)))
                                            .Where(x => x.UpdatedUtc <= command.ArchiveTime)
                                            .Select(x => ArchiveDeviceDto.CreateDto(x)).ToListAsync();

            RemoveSensitiveDetails(devices);

            return devices;
        }

        private void RemoveSensitiveDetails(IEnumerable<ArchiveDeviceDto> devices)
        {
            foreach (var device in devices)
            {
                if (string.IsNullOrWhiteSpace(device.DeviceContent))
                    continue;

                var deviceContentDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(device.DeviceContent);
                deviceContentDict = deviceContentDict.Where(x => VALID_BROKER_DETAIL_KEYS.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
                device.DeviceContent = JsonConvert.SerializeObject(deviceContentDict);
            }
        }

        public async Task<BaseResponse> RetrieveAsync(RetrieveDevice command, CancellationToken cancellationToken)
        {
            _userContext.SetUpn(command.Upn);
            var data = JsonConvert.DeserializeObject<ArchiveDeviceDataDto>(command.Data);
            if (!data.Devices.Any())
            {
                return BaseResponse.Success;
            }
            var brokerIds = command.AdditionalData.ToDictionary(x => Guid.Parse(x.Key), x => Guid.Parse(x.Value.ToString()));
            var entities = new List<Domain.Entity.Device>();
            var allAvailableBrokers = await GetAllAvailableBrokersAsync();
            foreach (var item in data.Devices.OrderBy(x => x.UpdatedUtc))
            {
                var device = await CreateFromArchivedAsync(item, command.Upn, brokerIds, allAvailableBrokers);
                entities.Add(device);
            }

            var retrieveTasks = new List<Task>();
            foreach (var device in entities)
            {
                retrieveTasks.Add(WaitAndDecorateDeviceAsync(device, cancellationToken));
            }
            await Task.WhenAll(retrieveTasks);

            await _deviceUnitOfWork.BeginTransactionAsync();
            try
            {
                await _deviceUnitOfWork.Devices.RetrieveAsync(entities);
                await _deviceUnitOfWork.CommitAsync();
            }
            catch
            {
                await _deviceUnitOfWork.RollbackAsync();
                throw;
            }

            // Needs to consider only send single event
            int totalDevice = await _deviceUnitOfWork.Devices.GetTotalDeviceAsync();

            var deviceAddedEventTasks = data.Devices
                .Select(x => new DeviceChangedEvent(x.Id, totalDevice, false, _tenantContext))
                .Select(x => _dispatcher.SendAsync(x));

            await Task.WhenAll(deviceAddedEventTasks);

            // Delete the redis cache.
            await _deviceBackgroundService.QueueAsync(
                _tenantContext,
                new CleanDeviceCache(
                    data.Devices.Select(x => x.Id)
                )
            );

            return BaseResponse.Success;
        }

        private async Task<Domain.Entity.Device> CreateFromArchivedAsync(
            ArchiveDeviceDto dto,
            string upn,
            IDictionary<Guid, Guid> brokerIds,
            IEnumerable<SharedBrokerDto> allAvailableBrokers
            )
        {
            var entity = ArchiveDeviceDto.CreateEntity(dto);
            entity.DeviceContent = null;

            var isEmqxBroker = false;

            if (!string.IsNullOrEmpty(dto.DeviceContent))
            {
                var deviceContent = JsonConvert.DeserializeObject<BrokerIotDeviceDto>(dto.DeviceContent);
                isEmqxBroker = BrokerConstants.EMQX_BROKERS.Contains(deviceContent.BrokerType ?? string.Empty);
                await ProcessDeviceContentAsync(deviceContent, brokerIds, allAvailableBrokers);
                entity.DeviceContent = JsonConvert.SerializeObject(deviceContent);
            }

            //Map topic
            if (isEmqxBroker)
            {
                if (!string.IsNullOrEmpty(entity.CommandTopic) && Regex.IsMatch(entity.CommandTopic, RegexConstants.PATTERN_COMMAND_TOPIC))
                {
                    entity.CommandTopic = Regex.Replace(entity.CommandTopic, RegexConstants.PATTERN_PROJECT_ID, _tenantContext.ProjectId);
                }

                if (!string.IsNullOrEmpty(entity.TelemetryTopic) && Regex.IsMatch(entity.TelemetryTopic, RegexConstants.PATTERN_TELEMETRY_TOPIC))
                {
                    entity.TelemetryTopic = Regex.Replace(entity.TelemetryTopic, RegexConstants.PATTERN_PROJECT_ID, _tenantContext.ProjectId);
                }
            }

            entity.CreatedBy = upn;

            return entity;
        }

        private async Task ProcessDeviceContentAsync(BrokerIotDeviceDto deviceContent, IDictionary<Guid, Guid> brokerIds, IEnumerable<SharedBrokerDto> allAvailableBrokers)
        {
            if (!deviceContent.BrokerId.HasValue)
                return;

            // BrokerId is regenerated on retrieve
            // So, we need to find the new ID from the list of given broker ID pairs.
            if (brokerIds.TryGetValue(deviceContent.BrokerId.Value, out var newBrokerId))
            {
                deviceContent.BrokerId = newBrokerId;
                // We might don't need this because we can always find projectId from the tenant context
                // But, some deviceContent still contain a projectId
                // So, we have to make it compatible
                deviceContent.ProjectId = _tenantContext.ProjectId;
            }
            else
            {
                try
                {
                    var sharedIoTHub = allAvailableBrokers.FirstOrDefault(x => x.Type == BrokerConstants.IOT_HUB &&
                                                                         x.Id == deviceContent.BrokerId);
                    if (sharedIoTHub != null)
                    {
                        deviceContent.ProjectId = sharedIoTHub.ProjectId;
                        if (await IsExistingIotDeviceAsync(deviceContent))
                        {
                            deviceContent.BrokerId = null;
                            deviceContent.ProjectId = _tenantContext.ProjectId;
                        }
                    }
                    else
                    {
                        deviceContent.BrokerId = null;
                    }
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    deviceContent.BrokerId = null;
                }
            }
        }

        private async Task<bool> IsExistingIotDeviceAsync(BrokerIotDeviceDto deviceContent)
        {
            var broker = await GetBrokerByIdAsync(deviceContent.BrokerId.Value, deviceContent.ProjectId);
            var brokerFunction = _httpClientFactory.CreateClient(HttpClientNames.BROKER_FUNCTION, _tenantContext);
            var response = await brokerFunction.PostAsync("fnc/bkr/device/exist", new StringContent(JsonConvert.SerializeObject(new Dictionary<string, object>(deviceContent)
                                {
                                    {"tenantId", _tenantContext.TenantId.ToString()},
                                    {"subscriptionId", _tenantContext.SubscriptionId.ToString()},
                                    {"brokerContent", broker.Content}
                                }), Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
                return true;

            var responseContent = await response.Content.ReadAsByteArrayAsync();
            var responsePayload = responseContent.Deserialize<DeviceCheckExistingResponseDto>();
            return responsePayload.Payload;
        }

        private async Task WaitAndDecorateDeviceAsync(
            Domain.Entity.Device entity,
            CancellationToken cancellationToken)
        {
            // Newly generated broker isn't ready immediately
            // So, we have to wait for ~ 30 minutes
            const int delayInMilliseconds = 5 * 1000; // 5seconds

            var waitTime = new CancellationTokenSource(15 * 60 * 1000);

            _logger.LogError($"RETRIEVE ENTITY: {JsonConvert.SerializeObject(entity)}");
            while (!waitTime.IsCancellationRequested)
            {
                var isCompletedSuccessfully = await DecorateDeviceAsync(entity)
                    .ContinueWith(t =>
                    {
                        return t.IsCompletedSuccessfully;
                    });

                if (isCompletedSuccessfully)
                    return;

                await Task.Delay(delayInMilliseconds);
            }

            throw EntityValidationExceptionHelper.GenerateException(nameof(BrokerIotDeviceDto.BrokerId), MessageConstants.ERROR_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED, detailCode: MessageConstants.DETAIL_CODE_DEVICE_RELATED_ITEMS_DELETED_OR_INACTIVATED);
        }

        public async Task<BaseResponse> VerifyArchiveAsync(VerifyDevice command, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<ArchiveDeviceDataDto>(command.Data);
            foreach (var device in data.Devices)
            {
                var validation = await _validator.ValidateAsync(device);
                if (!validation.IsValid)
                {
                    throw EntityValidationExceptionHelper.GenerateException(nameof(command.Data), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
                }
            }
            return BaseResponse.Success;
        }

        public override async Task<BaseSearchResponse<GetDeviceDto>> SearchAsync(GetDeviceByCriteria request)
        {
            var filters = new List<SearchFilter>
            {
                    new SearchFilter("d.deleted", "false", operation: "eq", "boolean")
            };
            var finalFilter = new SearchAndFilter(filters, request.Filter);

            request.Filter = finalFilter.ToJson();

            BaseSearchResponse<GetDeviceDto> response = await base.SearchAsync(request);
            return await _tagService.FetchTagsAsync(response);
        }

        protected override async Task RetrieveDataAsync(GetDeviceByCriteria criteria, BaseSearchResponse<GetDeviceDto> result)
        {
            var listResult = await _deviceUnitOfWork.Devices.GetDeviceAsync(criteria);
            if (listResult.Any())
            {
                result.AddRangeData(listResult);
            }
        }

        protected override async Task CountAsync(GetDeviceByCriteria criteria, BaseSearchResponse<GetDeviceDto> result) => result.TotalCount = await _deviceUnitOfWork.Devices.CountAsync(criteria);

        private static GetDeviceByCriteria MappingSearchTags(GetDeviceByCriteria criteria)
        {
            if (!string.IsNullOrEmpty(criteria.Filter))
            {
                SearchFilter filter = criteria.Filter.FromJson<SearchFilter>();
                RemoveTagsFilter(filter);
                criteria.Filter = filter.ToJson();
            }

            return criteria;
        }

        private static bool RemoveTagsFilter(SearchFilter filter)
        {
            if (filter.Or != null)
            {
                foreach (SearchFilter item in filter.Or)
                {
                    List<SearchFilter> toRemoves = new List<SearchFilter>();

                    foreach (SearchFilter item2 in filter.Or)
                    {
                        bool containTags = RemoveTagsFilter(item2);

                        if (containTags)
                        {
                            toRemoves.Add(item2);
                        }
                    }

                    filter.Or = filter.Or.Except(toRemoves).ToList();

                    if (!filter.Or.Any())
                        filter.Or = null;
                }
            }
            else if (filter.And != null)
            {
                List<SearchFilter> toRemoves = new List<SearchFilter>();

                foreach (SearchFilter item2 in filter.And)
                {
                    bool containTags = RemoveTagsFilter(item2);

                    if (containTags)
                    {
                        toRemoves.Add(item2);
                    }
                }

                filter.And = filter.And.Except(toRemoves).ToList();

                if (!filter.And.Any())
                    filter.And = null;
            }
            else
            {
                if (!string.IsNullOrEmpty(filter.QueryKey) && string.Compare(filter.QueryKey, "EntityTags.TagId", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return true;
                }
            }

            return false;
        }


        private async Task HandleRedisCacheWhenAddEntityAsync(Domain.Entity.Device device)
        {
            await _deviceBackgroundService.QueueAsync(_tenantContext, new AddDeviceToCache(device.Id));
        }

        private async Task HandleRedisCacheWhenEntityChangedAsync(string deviceId)
        {
            var deviceKey = CacheKey.DeviceInfoPattern.GetCacheKey(_tenantContext.ProjectId);
            await _cache.DeleteHashByKeyAsync(deviceKey, deviceId.ToString());
        }

        private async Task HandleRedisCacheWhenEntityChangedAsync(IEnumerable<string> deviceIds)
        {
            foreach (var deviceId in deviceIds)
                await HandleRedisCacheWhenEntityChangedAsync(deviceId);
        }

    }
}
