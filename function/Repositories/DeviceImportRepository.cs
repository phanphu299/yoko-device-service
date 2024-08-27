using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Dapper;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Model.ImportModel;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Import.Abstraction;
using System.Net.Http;
using AHI.Infrastructure.SharedKernel.Model;
using System.Linq;
using System;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Device.Function.Service.Abstraction;
using DisplayPropertyName = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.Device;
using DeviceContentKeys = AHI.Device.Function.Constant.JsonPayloadKeys.DeviceContent;
using AHI.Infrastructure.MultiTenancy.Extension;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using AHI.Infrastructure.UserContext.Abstraction;
using System.Text.RegularExpressions;
using System.Text;
using AHI.Infrastructure.Repository.Abstraction;
using AHI.Infrastructure.Service.Tag.Model;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using System.Data.Common;
using AHI.Infrastructure.SharedKernel.Abstraction;

namespace AHI.Infrastructure.Repository
{
    public class DeviceImportRepository : IImportRepository<DeviceModel>
    {
        private readonly ErrorType _errorType = ErrorType.DATABASE;
        public const string DEFAULT_RETENTION_DAYS = "default.metric.retention.days";
        public const int LOCAL_DEFAULT_RETENTION_DAYS = 90;
        private readonly ITenantContext _tenantContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IIotBrokerService _iotBrokerService;
        private readonly IProjectService _projectService;
        private readonly IImportTrackingService _errorService;
        private readonly IUserContext _userContext;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ITagService _tagService;
        private readonly ILoggerAdapter<DeviceImportRepository> _logger;
        private readonly TagValidator _tagValidator;

        public DeviceImportRepository(ITenantContext tenantContext,
            IHttpClientFactory factory,
            IDictionary<string, IImportTrackingService> errorHandlers,
            IUserContext userContext,
            IIotBrokerService iotBrokerService,
            IProjectService projectService,
            IDbConnectionFactory dbConnectionFactory,
            ITagService tagService,
            ILoggerAdapter<DeviceImportRepository> logger)
        {
            _tenantContext = tenantContext;
            _httpClientFactory = factory;
            _iotBrokerService = iotBrokerService;
            _projectService = projectService;
            _dbConnectionFactory = dbConnectionFactory;
            _errorService = errorHandlers[MimeType.EXCEL];
            _userContext = userContext;
            _tagService = tagService;
            _logger = logger;
            _tagValidator = new TagValidator(_errorService);
        }

        public Task CommitAsync(IEnumerable<DeviceModel> source)
        {
            throw new NotImplementedException();
        }

        public async Task CommitAsync(IEnumerable<DeviceModel> source, Guid correlationId)
        {
            _logger.LogInformation($"CorrelationId: {correlationId} | Starting DeviceImportRepository - CommitAsync");

            try
            {
                // if any error detected when parsing data in any sheet, discard all file
                if (_errorService.HasError)
                    return;
                using (var connection = _dbConnectionFactory.CreateConnection())
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        _logger.LogInformation($"CorrelationId: {correlationId} | Starting DeviceImportRepository - RemoveDuplicateDeviceAsync");
                        source = await RemoveDuplicateDeviceAsync(source, connection, transaction);

                        _logger.LogInformation($"CorrelationId: {correlationId} | Starting DeviceImportRepository - ValidateTemplateAsync");
                        if (!await ValidateTemplateAsync(source, connection, transaction))
                            return;
                    }
                    await connection.CloseAsync();
                }

                List<DeviceModel> deviceModels = source.ToList();

                if (deviceModels.Count == 0)
                {
                    _logger.LogInformation($"CorrelationId: {correlationId} | End DeviceImportRepository - CommitAsync - No devices");
                    return;
                }

                _logger.LogInformation($"CorrelationId: {correlationId} | Starting DeviceImportRepository - ValidateDeviceByBindingAsync");
                if (!await ValidateDeviceByBindingAsync(deviceModels))
                {
                    return;
                }

                _logger.LogInformation($"CorrelationId: {correlationId} | Starting DeviceImportRepository - ValidateTagsAsync");
                if (!ValidateTags(deviceModels))
                {
                    return;
                }

                _logger.LogInformation($"CorrelationId: {correlationId} | Starting DeviceImportRepository - PopulateDefaultRetentionDayAsync");
                await PopulateDefaultRetentionDayAsync(deviceModels);

                _logger.LogInformation($"CorrelationId: {correlationId} | Starting DeviceImportRepository - AddDeviceAsync");
                await AddDeviceAsync(deviceModels);

                _logger.LogInformation($"CorrelationId: {correlationId} | Starting DeviceImportRepository - AddTagsAsync");
                await AddTagsAsync(deviceModels);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"CorrelationId: {correlationId} | Error in DeviceImportRepository - CommitAsync {ex.Message}");
                throw;
            }
        }

        private async Task<IEnumerable<DeviceModel>> RemoveDuplicateDeviceAsync(IEnumerable<DeviceModel> devices, IDbConnection connection, IDbTransaction transaction)
        {
            var query = $"SELECT LOWER(id) FROM {DbName.Table.DEVICE} WHERE LOWER(id) = ANY (@DeviceIds)";
            var param = new
            {
                DeviceIds = devices.Select(x => x.Id.ToLowerInvariant()).ToArray()
            };
            var queryResult = await connection.QueryAsync<string>(query, param, transaction, commandTimeout: 600);
            var deviceIds = queryResult.ToHashSet();
            return devices.Where(device => !deviceIds.Contains(device.Id.ToLowerInvariant()));
        }

        private async Task<bool> ValidateTemplateAsync(IEnumerable<DeviceModel> devices, IDbConnection connection, IDbTransaction transaction)
        {
            var query = @"SELECT tmp.id, tmp.name, bnd.device_template_id IS NOT NULL AS HasBinding
                        FROM
                            v_template_valid tmp
                        LEFT JOIN
                            (SELECT DISTINCT device_template_id FROM template_bindings) bnd ON bnd.device_template_id = tmp.id
                        WHERE tmp.deleted = false AND LOWER(tmp.name) = ANY (@TemplateNames);";
            var param = new
            {
                TemplateNames = devices.Select(x => x.Template.ToLowerInvariant()).ToArray()
            };
            var queryResult = await connection.QueryAsync<(Guid Id, string Name, bool HasBinding)>(query, param, transaction, commandTimeout: 600);
            var templateInfos = queryResult.ToDictionary(x => x.Name.ToLowerInvariant());

            var success = true;
            foreach (var device in devices)
            {
                if (templateInfos.TryGetValue(device.Template.ToLowerInvariant(), out var templateInfo))
                {
                    device.TemplateId = templateInfo.Id;
                    device.HasBinding = templateInfo.HasBinding;
                }
                else
                {
                    success = false;
                    _errorService.RegisterError(ValidationMessage.NOT_EXIST, device, nameof(device.Template), _errorType, new Dictionary<string, object>
                    {
                        { "propertyName", DisplayPropertyName.TEMPLATE },
                        { "propertyValue", device.Template },
                    });
                }
            }
            return success;
        }

        private async Task<bool> ValidateDeviceByBindingAsync(IEnumerable<DeviceModel> devices)
        {
            var currentProject = await _projectService.GetCurrentProjectAsync();
            var brokers = await _iotBrokerService.SearchSharedBrokersAsync();
            var brokerLookups = brokers.Where(broker => broker.ProjectId == currentProject.Id).Select(broker => new Device.Function.Model.BrokerDto
            {
                Id = broker.Id,
                ProjectId = broker.ProjectId,
                // support import current project's brokers's name as both "brokerName" or "currentProjectName/brokerName"
                Name = $"{currentProject.Name}/{broker.Name}",
                Type = broker.Type
            }).Union(brokers).ToDictionary(broker => broker.Name.ToLowerInvariant(), broker => (broker.Id, broker.ProjectId, broker.Type));

            var isValid = true;
            foreach (var device in devices)
            {
                if (string.IsNullOrWhiteSpace(device.BrokerName))
                    continue;

                if (!brokerLookups.TryGetValue(device.BrokerName.ToLowerInvariant(), out var broker))
                {
                    isValid = false;
                    _errorService.RegisterError(ValidationMessage.NOT_EXIST, device, nameof(device.BrokerName), _errorType, new Dictionary<string, object>
                    {
                        { "propertyName", DisplayPropertyName.BROKER_NAME },
                        { "propertyValue", device.BrokerName },
                    });
                    continue;
                }

                if (BrokerConstants.EMQX_BROKERS.Contains(broker.Type))
                {
                    isValid = ValidateTokenDuration(device, device.TokenDuration, DisplayPropertyName.TOKEN_DURATION, nameof(device.TokenDuration));
                    isValid = isValid ? IsValidTopicName(device) : isValid;
                }
                else
                {
                    isValid = ValidateTokenDuration(device, device.SasTokenDuration, DisplayPropertyName.SAS_TOKEN_DURATION, nameof(device.SasTokenDuration));
                }

                if (broker.Type == BrokerConstants.IOT_HUB &&
                    !Regex.IsMatch(device.Id, RegexConstants.IOT_HUB_DEVICE_ID))
                {
                    isValid = false;
                    _errorService.RegisterError(ValidationMessage.DEVICE_ID_UNMATCHED_BROKER_TYPE_LOG, device, nameof(device.Id), ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyName", DisplayPropertyName.ID },
                        { "propertyValue", device.Id },
                    });
                    continue;
                }

                if (!isValid)
                    continue;

                device.BrokerId = broker.Id;
                device.BrokerProjectId = broker.ProjectId;
                device.BrokerType = broker.Type;
            }
            return isValid;
        }

        private bool IsValidTopicName(DeviceModel device)
        {
            var telemetryTopicRegex = new Regex(RegexConstants.PATTERN_TELEMETRY_TOPIC);
            var commandTopicRegex = new Regex(RegexConstants.PATTERN_COMMAND_TOPIC);

            // validate telemetry topic
            if (string.IsNullOrEmpty(device.TelemetryTopic))
            {
                _errorService.RegisterError(ValidationMessage.REQUIRED, device, nameof(device.TelemetryTopic), ErrorType.VALIDATING, new Dictionary<string, object>
                {
                    { "propertyName", DisplayPropertyName.TELEMETRY_TOPIC }
                });

                return false;
            }
            else if (!telemetryTopicRegex.IsMatch(device.TelemetryTopic))
            {
                _errorService.RegisterError(ValidationMessage.DEVICE_INVALID_TOPIC_FORMAT, device, nameof(device.TelemetryTopic), ErrorType.VALIDATING, new Dictionary<string, object>
                {
                    { "propertyName", DisplayPropertyName.TELEMETRY_TOPIC },
                    { "propertyValue", device.TelemetryTopic },
                });
                return false;
            }
            else if (Encoding.UTF8.GetBytes(device.TelemetryTopic).Length > NumberConstains.MAX_LENGTH_BYTE)
            {
                _errorService.RegisterError(ValidationMessage.MAX_LENGTH_UTF8, device, nameof(device.TelemetryTopic), ErrorType.VALIDATING, new Dictionary<string, object>
                {
                    { "propertyName", DisplayPropertyName.TELEMETRY_TOPIC },
                    { "maxLength", NumberConstains.MAX_LENGTH_BYTE },
                });
                return false;
            }

            // validate command topic
            if (device.HasCommand == true)
            {
                if (string.IsNullOrEmpty(device.CommandTopic))
                {
                    _errorService.RegisterError(ValidationMessage.REQUIRED, device, nameof(device.CommandTopic), ErrorType.VALIDATING, new Dictionary<string, object>

                    {
                        { "propertyName", DisplayPropertyName.COMMAND_TOPIC }
                    });
                    return false;
                }
                else if (!commandTopicRegex.IsMatch(device.CommandTopic))
                {
                    _errorService.RegisterError(ValidationMessage.DEVICE_INVALID_TOPIC_FORMAT, device, nameof(device.CommandTopic), ErrorType.VALIDATING, new Dictionary<string, object>
                {
                    { "propertyName", DisplayPropertyName.COMMAND_TOPIC },
                    { "propertyValue", device.CommandTopic },
                });

                    return false;
                }
                else if (Encoding.UTF8.GetBytes(device.CommandTopic).Length > NumberConstains.MAX_LENGTH_BYTE)
                {
                    _errorService.RegisterError(ValidationMessage.MAX_LENGTH_UTF8, device, nameof(device.CommandTopic), ErrorType.VALIDATING, new Dictionary<string, object>
                {
                    { "propertyName", DisplayPropertyName.COMMAND_TOPIC },
                    { "maxLength", NumberConstains.MAX_LENGTH_BYTE },
                });

                    return false;
                }
            }

            return true;
        }

        private bool ValidateTokenDuration(
            DeviceModel device,
            int? tokenDuration,
            string displayPropertyName,
            string propertyName)
        {
            if (!tokenDuration.HasValue)
            {
                _errorService.RegisterError(ValidationMessage.REQUIRED, device, propertyName, ErrorType.VALIDATING, new Dictionary<string, object>
                {
                    { "propertyName", displayPropertyName }
                });
                return false;
            }

            if (tokenDuration.Value < 1)
            {
                _errorService.RegisterError(ValidationMessage.LESS_THAN_MIN_VALUE, device, propertyName, ErrorType.VALIDATING, new Dictionary<string, object>
                {
                    { "propertyName", displayPropertyName },
                    { "propertyValue", tokenDuration.Value },
                    { "comparisonValue", 1 }
                });
                return false;
            }

            if (tokenDuration.Value > 3650)
            {
                _errorService.RegisterError(ValidationMessage.GREATER_THAN_MAX_VALUE, device, propertyName, ErrorType.VALIDATING, new Dictionary<string, object>
                {
                    { "propertyName", displayPropertyName },
                    { "propertyValue", tokenDuration.Value },
                    { "comparisonValue", 3650 }
                });
                return false;
            }

            return true;
        }

        private async Task AddDeviceAsync(IEnumerable<DeviceModel> source)
        {
            var deviceClient = _httpClientFactory.CreateClient(ClientNameConstant.DEVICE_SERVICE, _tenantContext);
            var message = "Failed to add device.";
            foreach (var device in source)
            {
                device.CreatedBy = _userContext.Upn;
                ApplyProjectIdForTopicName(device);
                var request = AddDeviceRequest.Create(device);
                try
                {
                    var response = await deviceClient.PostAsync("dev/devices", new StringContent(request.ToJson(), System.Text.Encoding.UTF8, mediaType: "application/json"));
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        var content = await response.Content.ReadAsByteArrayAsync();
                        var error = content.Deserialize<Newtonsoft.Json.Linq.JObject>();
                        message = string.Join(' ', message, error.Value<string>("message"));
                    }
                    response.EnsureSuccessStatusCode();
                }
                catch
                {
                    _errorService.RegisterError(message, device, nameof(device.Id), _errorType);
                    break;
                }
            }
        }

        private bool ValidateTags(List<DeviceModel> deviceModels)
        {
            bool success = true;

            foreach (DeviceModel deviceModel in deviceModels)
            {
                if (string.IsNullOrEmpty(deviceModel.Tags))
                {
                    continue;
                }

                if (!_tagValidator.ValidateTags(deviceModel.Tags, deviceModel))
                {
                    success = false;
                    break;
                }
            }

            return success;
        }

        private async Task AddTagsAsync(List<DeviceModel> deviceModels)
        {
            bool success = true;

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (DeviceModel deviceModel in deviceModels)
                        {
                            if (string.IsNullOrEmpty(deviceModel.Tags))
                            {
                                continue;
                            }

                            var upsertTag = new UpsertTagCommand
                            {
                                Upn = _userContext.Upn,
                                ApplicationId = Guid.Parse(ApplicationInformation.APPLICATION_ID),
                                IgnoreNotFound = true, // NOTE: DO NOT ADD NEW TAG IF NOT EXIST IN PROJECT
                                Tags = deviceModel.Tags.Split(TagConstants.TAG_IMPORT_EXPORT_SEPARATOR)
                                                        .Where(tag => !string.IsNullOrWhiteSpace(tag))
                                                        .Select(tag => new UpsertTag
                                                        {
                                                            Key = tag.Split(":")[0].Trim(),
                                                            Value = tag.Split(":")[1].Trim()
                                                        })
                            };
                            var tagIds = await _tagService.UpsertTagsAsync(upsertTag);

                            if (tagIds.Any())
                            {
                                var updateTagBindingQuery = $@"
                                                    INSERT INTO {DbName.Table.ENTITY_TAG} (entity_id_varchar, entity_type, tag_id)
                                                    VALUES (@Id, @EntityType, @TagId)";
                                await connection.ExecuteAsync(updateTagBindingQuery, tagIds.Select(tagId => new { Id = deviceModel.Id, EntityType = "device", TagId = tagId }), transaction, commandTimeout: 600);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        _errorService.RegisterError($"Data not valid {ex.Message}", _errorType);
                        success = false;
                    }

                    await (success ? transaction.CommitAsync() : transaction.RollbackAsync());
                }

                await connection.CloseAsync();
            }
        }

        private async Task PopulateDefaultRetentionDayAsync(IEnumerable<DeviceModel> devices)
        {
            var defaultValue = await GetDefaultRetentionDayAsync();
            foreach (var device in devices)
            {
                if (!device.RetentionDays.HasValue)
                    device.RetentionDays = defaultValue;
            }
        }

        private async Task<int> GetDefaultRetentionDayAsync()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient(ClientNameConstant.CONFIGURATION_SERVICE, _tenantContext);
                var response = await httpClient.GetAsync($"cnm/configs?key={DEFAULT_RETENTION_DAYS}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsByteArrayAsync();
                var message = content.Deserialize<BaseSearchResponse<SystemConfig>>();
                return Convert.ToInt32(message.Data.ElementAt(0).Value);
            }
            catch
            {
                return LOCAL_DEFAULT_RETENTION_DAYS;
            }
        }

        private void ApplyProjectIdForTopicName(DeviceModel device)
        {
            if (string.Equals(device.BrokerType, BrokerTypeConstants.EMQX_MQTT)
                || string.Equals(device.BrokerType, BrokerTypeConstants.EMQX_COAP))
            {
                if (!string.IsNullOrEmpty(device.TelemetryTopic))
                {
                    device.TelemetryTopic = Regex.Replace(device.TelemetryTopic, RegexConstants.PATTERN_PROJECT_ID, _tenantContext.ProjectId);
                }

                if (!string.IsNullOrEmpty(device.CommandTopic))
                {
                    device.CommandTopic = Regex.Replace(device.CommandTopic, RegexConstants.PATTERN_PROJECT_ID, _tenantContext.ProjectId);
                }
            }
        }

        class SystemConfig
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public SystemConfig(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }

        class AddDeviceRequest
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public Guid TemplateId { get; set; }
            public int RetentionDays { get; set; }
            public string DeviceContent { get; set; }
            public string CreatedBy { get; set; }
            public string TelemetryTopic { get; set; }
            public string CommandTopic { get; set; }
            public bool? HasCommand { get; set; }

            public static AddDeviceRequest Create(DeviceModel device)
            {
                return new AddDeviceRequest
                {
                    Id = device.Id,
                    Name = device.Name,
                    TemplateId = device.TemplateId.Value,
                    RetentionDays = device.RetentionDays.Value,
                    DeviceContent = BuildContent(device),
                    CreatedBy = device.CreatedBy,
                    TelemetryTopic = device.TelemetryTopic,
                    CommandTopic = device.CommandTopic,
                    HasCommand = device.HasCommand,
                };
            }

            private static string BuildContent(DeviceModel device)
            {
                var content = new Dictionary<string, string>
                {
                    { DeviceContentKeys.DEVICE_ID, device.Id }
                };
                if (device.BrokerId.HasValue)
                {
                    content[DeviceContentKeys.BROKER_ID] = device.BrokerId.Value.ToString();
                    content[DeviceContentKeys.BROKER_TYPE] = device.BrokerType;
                    content[DeviceContentKeys.BROKER_PROJECT_ID] = device.BrokerProjectId;
                    content[DeviceContentKeys.SAS_TOKEN_DURATION] = device.SasTokenDuration.HasValue ? device.SasTokenDuration.Value.ToString() : null;
                    content[DeviceContentKeys.TOKEN_DURATION] = device.TokenDuration.HasValue ? device.TokenDuration.Value.ToString() : null;
                }
                return content.ToJson();
            }
        }
    }
}