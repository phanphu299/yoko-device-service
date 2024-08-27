using System.Threading.Tasks;
using System.Collections.Generic;
using AHI.Device.Function.Model.ImportModel;
using AHI.Infrastructure.Import.Abstraction;
using Dapper;
using System.Data;
using AHI.Device.Function.Constant;
using System.Data.Common;
using System;
using AHI.Device.Function.FileParser.Abstraction;
using Function.Extension;
using System.Linq;
using DisplayPropertyName = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.DeviceTemplate;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Json;
using Function.Enum;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using System.Text.RegularExpressions;
using AHI.Infrastructure.Repository.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using AHI.Infrastructure.UserContext.Abstraction;
using AHI.Infrastructure.Service.Tag.Model;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;

namespace AHI.Infrastructure.Repository
{
    public class DeviceTemplateRepository : IImportRepository<DeviceTemplate>
    {
        private readonly ErrorType _errorType = ErrorType.DATABASE;
        private readonly IImportTrackingService _errorService;
        private readonly NameValidator _nameValidator;
        private readonly JsonKeyExtractor _keyExtractor;
        private readonly IDynamicResolver _dynamicResolver;
        private readonly ILoggerAdapter<DeviceTemplateRepository> _logger;
        private readonly IUserContext _userContext;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ITagService _tagService;
        private readonly TagValidator _tagValidator;
        private readonly int DEFAULT_KEY_TYPE_ID_METRIC = 2;

        public DeviceTemplateRepository(
            IDictionary<string, IImportTrackingService> errorHandlers,
            IUserContext userContext,
            IDynamicResolver dynamicResolver,
            ILoggerAdapter<DeviceTemplateRepository> logger,
            IDbConnectionFactory dbConnectionFactory,
            ITagService tagService)
        {
            _errorService = errorHandlers[MimeType.JSON];
            _keyExtractor = new JsonKeyExtractor();
            _nameValidator = new NameValidator(DbName.Table.DEVICE_TEMPLATE, "name");
            _nameValidator.Seperator = ' ';
            _dynamicResolver = dynamicResolver;
            _logger = logger;
            _dbConnectionFactory = dbConnectionFactory;
            _userContext = userContext;
            _tagService = tagService;
            _tagValidator = new TagValidator(_errorService);
        }

        public async Task CommitAsync(IEnumerable<DeviceTemplate> source, Guid correlationId)
        {
            _logger.LogInformation($"CorrelationId: {correlationId} | Starting DeviceTemplateRepository - CommitAsync");

            try
            {
                // if any error detected when parsing data in any sheet, discard all file
                if (_errorService.HasError)
                    return;

                bool success = true;
                using (var connection = _dbConnectionFactory.CreateConnection())
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        _logger.LogInformation($"CorrelationId: {correlationId} | Starting DeviceTemplateRepository - GetListDeviceTemplateAsync");
                        var dbDeviceTemplates = await GetListDeviceTemplateAsync(source, connection, transaction);
                        foreach (var template in source)
                        {
                            MapTemplatePayloadDetailId(dbDeviceTemplates, template);
                        }

                        _logger.LogInformation($"CorrelationId: {correlationId} | Starting DeviceTemplateRepository - ValidateTemplateAsync");
                        if (!await ValidateTemplateAsync(source, connection, transaction))
                            return;

                        foreach (DeviceTemplate template in source)
                        {
                            try
                            {
                                var deviceTemplate = dbDeviceTemplates.FirstOrDefault(x => string.Equals(x.Name, template.Name, StringComparison.InvariantCultureIgnoreCase));
                                Guid templateId;

                                if (deviceTemplate != null)
                                {
                                    await UpdateExistingTemplateAsync(deviceTemplate, template, connection, transaction);
                                    templateId = deviceTemplate.Id;
                                }
                                else
                                {
                                    templateId = await AddNewTemplateAsync(template, connection, transaction);
                                }

                                var deleteTagBindingQuery = $@"DELETE FROM {DbName.Table.ENTITY_TAG} WHERE entity_id_uuid = '{templateId}' AND entity_type = '{IOEntityType.DEVICE_TEMPLATE}'";
                                await connection.ExecuteAsync(deleteTagBindingQuery, transaction, commandTimeout: 600);

                                if (template.Tags != null && template.Tags.Any())
                                {
                                    // List with tags where both Key and Value are empty
                                    var emptyItems = template.Tags.Where(item => string.IsNullOrWhiteSpace(item.Key) && string.IsNullOrWhiteSpace(item.Value)).ToList();

                                    // New list excluding tags where both Key and Value are empty
                                    template.Tags = template.Tags.Except(emptyItems).ToList();
                                }

                                if (template.Tags.Any())
                                {
                                    _logger.LogInformation($"CorrelationId: {correlationId} | Starting DeviceTemplateRepository - ValidateTags");
                                    if (!_tagValidator.ValidateTags(template.Tags, template))
                                    {
                                        success = false;
                                        continue;
                                    }

                                    List<UpsertTag> upsertTags = new List<UpsertTag>();

                                    foreach (var tag in template.Tags)
                                    {
                                        if (!upsertTags.Exists(u => u.Key == tag.Key.Trim() && u.Value == tag.Value.Trim()))
                                        {
                                            upsertTags.Add(new UpsertTag
                                            {
                                                Key = tag.Key.Trim(),
                                                Value = tag.Value.Trim()
                                            });
                                        }
                                    }

                                    var upsertTag = new UpsertTagCommand
                                    {
                                        Upn = _userContext.Upn,
                                        ApplicationId = Guid.Parse(ApplicationInformation.APPLICATION_ID),
                                        IgnoreNotFound = true, // NOTE: DO NOT ADD NEW TAG IF NOT EXIST IN PROJECT
                                        Tags = upsertTags
                                    };
                                    List<long> tagIds = (await _tagService.UpsertTagsAsync(upsertTag)).ToList();

                                    if (tagIds.Any())
                                    {
                                        _logger.LogInformation($"CorrelationId: {correlationId} | Starting DeviceTemplateRepository - Upsert Tags");

                                        var updateTagBindingQuery = $@"
                                                        INSERT INTO {DbName.Table.ENTITY_TAG} (entity_id_uuid, entity_type, tag_id)
                                                        VALUES (@Id, @EntityType, @TagId)";
                                        await connection.ExecuteAsync(updateTagBindingQuery, tagIds.Select(tagId => new { Id = templateId, EntityType = IOEntityType.DEVICE_TEMPLATE, TagId = tagId }), transaction, commandTimeout: 600);
                                    }
                                }
                            }
                            catch (DbException ex)
                            {
                                _logger.LogError(ex, $"CorrelationId: {correlationId} | Error in DeviceTemplateRepository - CommitAsync: " + ex.Message);
                                _errorService.RegisterError($"Data not valid {ex.Message}", _errorType);
                                success = false;
                            }
                        }
                        await (success ? transaction.CommitAsync() : transaction.RollbackAsync());
                    }
                    await connection.CloseAsync();
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"CorrelationId: {correlationId} | Error in DeviceTemplateRepository - CommitAsync: " + ex.Message);
                throw;
            }
        }

        public Task CommitAsync(IEnumerable<DeviceTemplate> source)
        {
            throw new NotImplementedException();
        }

        private void MapTemplatePayloadDetailId(IEnumerable<DeviceTemplateDb> dbDeviceTemplates, DeviceTemplate template)
        {
            var dbTemplate = dbDeviceTemplates.FirstOrDefault(x => string.Equals(x.Name, template.Name, StringComparison.InvariantCultureIgnoreCase));
            if (dbTemplate == null)
                return;

            foreach (var detail in template.Payloads.SelectMany(x => x.Details))
            {
                var currentDetail = dbTemplate.Payloads.SelectMany(x => x.Details).FirstOrDefault(x => x.Key == detail.Key);
                if (currentDetail != null)
                    detail.DetailId = currentDetail.DetailId;
            }
        }

        private async Task UpdateExistingTemplateAsync(DeviceTemplateDb deviceTemplate, DeviceTemplate template, IDbConnection connection, IDbTransaction transaction)
        {
            var detailAdds = new List<TemplateDetail>();
            var detailUpdates = new List<DeviceTemplateDetails>();
            var payloadInSides = new List<int>();
            foreach (var payload in template.Payloads)
            {
                var payloadId = await UpsertTemplatePayloadAsync(payload, deviceTemplate, payloadInSides, connection, transaction);
                ProcessTemplatePayloadDetails(payload, deviceTemplate, payloadId, detailAdds, detailUpdates);
            }
            // update payload not exists in json file
            var payloadOutSides = deviceTemplate.Payloads.Where(x => !payloadInSides.Contains(x.PayloadId)).ToList();
            foreach (var payload in payloadOutSides)
            {
                payload.JsonPayload = "{\"\": \"\"}";
                await UpdateTemplatePayloadAsync(payload, connection, transaction);
            }

            if (detailAdds.Any())
                await AddTemplateDetailAsync(detailAdds, connection, transaction);

            if (detailUpdates.Any())
                await UpdateTemplateDetailAsync(detailUpdates, connection, transaction);

            // update device template
            var totalMetric = await GetTotalMetricAsync(deviceTemplate.Id, connection, transaction);
            await UpdateTemplateAsync(totalMetric, deviceTemplate.Id, connection, transaction);
        }

        private async Task<int> UpsertTemplatePayloadAsync(TemplatePayload payload,
                                                           DeviceTemplateDb deviceTemplate,
                                                           List<int> payloadInSides,
                                                           IDbConnection connection,
                                                           IDbTransaction transaction)
        {
            var keys = payload.Details.Select(x => x.Key);
            var payloadId = 0;
            foreach (var key in keys)
            {
                var currentPayload = deviceTemplate.Payloads.FirstOrDefault(x => JsonObject.Parse(x.JsonPayload).ContainsKey(key));
                if (currentPayload != null)
                {
                    payloadId = currentPayload.PayloadId;
                    break;
                }
            }
            if (payloadId > 0)
            {
                payloadInSides.Add(payloadId);
                var currentPayload = deviceTemplate.Payloads.FirstOrDefault(x => x.PayloadId == payloadId);
                if (currentPayload != null)
                    currentPayload.JsonPayload = payload.JsonPayload;
                await UpdateTemplatePayloadAsync(currentPayload, connection, transaction);
            }
            else
            {
                payload.TemplateId = deviceTemplate.Id;
                payloadId = await AddTemplatePayloadAsync(payload, connection, transaction);
            }
            return payloadId;
        }

        private void ProcessTemplatePayloadDetails(TemplatePayload payload,
                                                   DeviceTemplateDb deviceTemplate,
                                                   int payloadId,
                                                   List<TemplateDetail> detailAdds,
                                                   List<DeviceTemplateDetails> detailUpdates)
        {
            foreach (var detail in payload.Details)
            {
                var currentDetail = deviceTemplate.Payloads.SelectMany(x => x.Details).FirstOrDefault(x => x.Key == detail.Key);
                if (currentDetail == null)
                {
                    detail.TemplatePayloadId = payloadId;
                    detailAdds.Add(detail);
                }
                else
                {
                    UpdateTemplateDetail(currentDetail, detail);
                    detailUpdates.Add(currentDetail);
                }
            }
        }

        private async Task<Guid> AddNewTemplateAsync(DeviceTemplate template, IDbConnection connection, IDbTransaction transaction)
        {
            template.Name = await ValidateDuplicateNameAsync(connection, template);
            template.CreatedBy = _userContext.Upn;
            var templateId = await AddTemplateAsync(template, connection, transaction);
            foreach (var payload in template.Payloads)
            {
                payload.TemplateId = templateId;

                var payloadId = await AddTemplatePayloadAsync(payload, connection, transaction);
                foreach (var detail in payload.Details)
                    detail.TemplatePayloadId = payloadId;
            }
            foreach (var binding in template.Bindings)
            {
                binding.TemplateId = templateId;
            }

            await AddTemplateDetailAsync(template.Payloads.SelectMany(payload => payload.Details), connection, transaction);
            await AddTemplateBindingAsync(template.Bindings, connection, transaction);

            // update device template
            var totalMetric = await GetTotalMetricAsync(templateId, connection, transaction);
            await UpdateTemplateAsync(totalMetric, templateId, connection, transaction);

            return templateId;
        }

        private void UpdateTemplateDetail(DeviceTemplateDetails currentDetail, TemplateDetail updateDetail)
        {
            currentDetail.Name = updateDetail.Name;
            currentDetail.Enabled = updateDetail.Enabled;
            currentDetail.Expression = updateDetail.Expression;
            currentDetail.DataType = updateDetail.DataType;
            currentDetail.ExpressionCompile = updateDetail.ExpressionCompile;
            currentDetail.KeyTypeId = updateDetail.KeyTypeId.HasValue
                                        ? updateDetail.KeyTypeId.Value
                                        : DEFAULT_KEY_TYPE_ID_METRIC;
        }

        private async Task<bool> ValidateTemplateAsync(IEnumerable<DeviceTemplate> templates, IDbConnection connection, IDbTransaction transaction)
        {
            var success = true;
            var metadata = await GetMetadataAsync(connection, transaction);
            foreach (var template in templates)
            {
                var details = template.Payloads.SelectMany(payload => payload.Details);
                var isValid = await ValidateDetailsAsync(connection, template.Name, details, metadata.KeyTypes, transaction)
                            && ValidateBindings(template.Bindings, metadata.DataTypes)
                            && ValidateJsonPayloads(template.Payloads);

                if (!isValid)
                    success = false;
            }
            return success;
        }

        private async Task<(IEnumerable<MetadataInfo> DataTypes, IEnumerable<MetadataInfo> KeyTypes)> GetMetadataAsync(IDbConnection connection, IDbTransaction transaction)
        {
            var query = @"SELECT id, name FROM data_types; SELECT id, name FROM template_key_types;";
            var queryResult = await connection.QueryMultipleAsync(query, transaction: transaction, commandTimeout: 600);

            var datatypes = await queryResult.ReadAsync<MetadataInfo>();
            var keytypes = await queryResult.ReadAsync<MetadataInfo>();

            // replace aggregation with calculation
            var aggregationType = keytypes.FirstOrDefault(keytype => keytype.Name == TemplateKeyTypes.AGGREGATION);
            if (aggregationType != null)
                aggregationType.Name = TemplateKeyTypes.CALCULATION;

            // add device_id type with same id as text type
            var textType = datatypes.FirstOrDefault(datatype => datatype.Name == DataTypeConstants.TYPE_TEXT);
            if (textType != null)
                datatypes = datatypes.Append(new MetadataInfo { Id = textType.Id, Name = DataTypeConstants.TYPE_DEVICEID });

            return (datatypes, keytypes);
        }

        private async Task<bool> ValidateDetailsAsync(IDbConnection connection, string templateName, IEnumerable<TemplateDetail> details, IEnumerable<MetadataInfo> keyTypes, IDbTransaction transaction)
        {
            var isTypesValid = true;
            var expressionValid = true;
            foreach (var detail in details)
            {
                var isKeyTypeValid = ValidateType<TemplateDetail>(detail, keyTypes, detail => detail.KeyType, (detail, id) => detail.KeyTypeId = id, DisplayPropertyName.KEY_TYPE);

                if (!isKeyTypeValid)
                    isTypesValid = false;

                if (detail.KeyType == TemplateKeyTypes.CALCULATION)
                {
                    var deviceTemplateId = await GetDeviceTemplateIdByNameAsync(templateName, connection, transaction);
                    var response = await ValidateExpressionAsync(connection, deviceTemplateId, detail, details);
                    expressionValid &= response.Item1;
                    if (!expressionValid)
                        _errorService.RegisterError(ValidationMessage.GENERAL_INVALID_VALUE, ErrorType.VALIDATING, new Dictionary<string, object>
                        {
                            { "propertyName", DisplayPropertyName.EXPRESSION },
                            { "propertyValue", detail.Expression }
                        });
                }
            }

            var isMetricNameUnique = ValidateDuplicateMetricName(details);

            var isKeysUnique = ValidateDuplicateKey<TemplateDetail>(details, detail => detail.Key);
            return isTypesValid && isKeysUnique && expressionValid && isMetricNameUnique;
        }

        private bool ValidateBindings(IEnumerable<TemplateBinding> bindings, IEnumerable<MetadataInfo> DataTypes)
        {
            bool isTypesValid = true;
            foreach (var binding in bindings)
            {
                var isValid = ValidateType<TemplateBinding>(binding, DataTypes, binding => binding.DataType, (binding, id) => binding.DataTypeId = id, DisplayPropertyName.DATA_TYPE);
                if (isValid)
                    binding.DefaultValue = binding.DefaultValue.ConvertTo(binding.DataType).ToString();
                else
                    isTypesValid = false;
            }
            var isKeysUnique = ValidateDuplicateKey<TemplateBinding>(bindings, binding => binding.Key);
            return isTypesValid && isKeysUnique;
        }

        private bool ValidateType<T>(T entity, IEnumerable<MetadataInfo> typeInfos, Func<T, string> fieldSelector, Action<T, int> fieldSetter, string errorName = null)
        {
            var typename = fieldSelector.Invoke(entity);
            var type = typeInfos.FirstOrDefault(type => type.Name == typename);
            if (type is null)
            {
                _errorService.RegisterError(ValidationMessage.NOT_EXIST, ErrorType.VALIDATING, new Dictionary<string, object>
                {
                    { "propertyName", errorName },
                    { "propertyValue", typename },
                });
                return false;
            }
            fieldSetter.Invoke(entity, type.Id);
            return true;
        }

        private bool ValidateDuplicateKey<T>(IEnumerable<T> entities, Func<T, string> keySelector)
        {
            bool isValid = true;
            HashSet<string> keys = new HashSet<string>();
            foreach (var entity in entities)
            {
                var key = keySelector.Invoke(entity);
                if (keys.Contains(key))
                {
                    _errorService.RegisterError(ValidationMessage.METRIC_DUPLICATED, ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyName", DisplayPropertyName.KEY },
                        { "propertyValue", key }
                    });
                    isValid = false;
                }
                else
                    keys.Add(key);
            }
            return isValid;
        }

        private bool ValidateDuplicateMetricName(IEnumerable<TemplateDetail> details)
        {
            bool isValid = true;
            var detailGroups = details.GroupBy(x => x.Name).Select(x => x.ToList());
            foreach (var g in detailGroups)
            {
                if (g.Count > 1)
                {
                    _errorService.RegisterError(ValidationMessage.METRIC_DUPLICATED, ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyName", DisplayPropertyName.NAME },
                        { "propertyValue", g.First().Name }
                    });
                    isValid = false;
                }
            }
            return isValid;
        }

        private bool ValidateJsonPayloads(IEnumerable<TemplatePayload> payloads)
        {
            bool isValid = true;
            var index = 0;
            var totalPayloadKeys = new HashSet<string>();
            foreach (var payload in payloads)
            {
                using (var reader = new System.IO.StringReader(payload.JsonPayload))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var payloadObject = JObject.Load(jsonReader);
                    var currentPayloadKeys = _keyExtractor.ExtractKeys(payloadObject);
                    currentPayloadKeys.ExceptWith(totalPayloadKeys);
                    if (!IsPayloadMatch(currentPayloadKeys.ToList(), payload.Details, index))
                        isValid = false;
                    totalPayloadKeys.UnionWith(currentPayloadKeys);
                }
                index++;
            }
            return isValid;
        }

        private bool IsPayloadMatch(IList<string> payloadKeys, IEnumerable<TemplateDetail> details, int index)
        {
            var detailKeys = details.Where(detail => detail.KeyType != TemplateKeyTypes.CALCULATION).Select(detail => detail.Key).ToList();
            var isMatch = true;
            if (index == 0 && detailKeys.Count != payloadKeys.Count)
            {
                _errorService.RegisterError(ValidationMessage.METRIC_INCONSISTENT, ErrorType.VALIDATING, new Dictionary<string, object>
                {
                    { "payloadPropertyName", $"{DisplayPropertyName.PAYLOAD} {index + 1}" },
                    { "detailPropertyName", DisplayPropertyName.DETAIL },
                });
                return false;
            }
            foreach (var key in payloadKeys)
            {
                if (!detailKeys.Contains(key))
                {
                    _errorService.RegisterError(ValidationMessage.METRIC_INCONSISTENT, ErrorType.VALIDATING, new Dictionary<string, object>
                {
                    { "payloadPropertyName", $"{DisplayPropertyName.PAYLOAD} {index + 1}" },
                    { "detailPropertyName", DisplayPropertyName.DETAIL },
                });
                    isMatch = false;
                    break;
                }
            }
            return isMatch;
        }

        private Task<string> ValidateDuplicateNameAsync(IDbConnection connection, DeviceTemplate template)
        {
            return _nameValidator.ValidateDuplicateNameAsync(template.Name, connection);
        }

        public async Task<Guid> GetDeviceTemplateIdByNameAsync(string name, IDbConnection connection, IDbTransaction transaction)
        {
            var query = $@"SELECT id FROM device_templates WHERE name ILIKE @Name";
            return await connection.QueryFirstOrDefaultAsync<Guid>(query, new { Name = name }, transaction, commandTimeout: 600);
        }

        public async Task<int> GetTotalMetricAsync(Guid templateId, IDbConnection connection, IDbTransaction transaction)
        {
            var query = $@"select count(*)
                           from template_details td
                           inner join template_payloads tp on td.template_payload_id = tp.id
                           inner join device_templates dt on tp.device_template_id = dt.id
                           where td.enabled = true and (td.key_type_id = {(int)TemplateKeyTypeEnum.Metric} or td.key_type_id = {(int)TemplateKeyTypeEnum.Aggregation}) and dt.id = @TemplateId;";
            return await connection.QueryFirstAsync<int>(query, new { TemplateId = templateId }, transaction, commandTimeout: 600);
        }

        private async Task<IEnumerable<DeviceTemplateDb>> GetListDeviceTemplateAsync(IEnumerable<DeviceTemplate> templates, IDbConnection connection, IDbTransaction transaction)
        {
            var query = $@"select
                                dt.id as {nameof(DeviceTemplateDb.Id)},
                                dt.name as {nameof(DeviceTemplateDb.Name)},
                                tp.id as {nameof(DeviceTemplatePayloads.PayloadId)},
                                tp.json_payload as {nameof(DeviceTemplatePayloads.JsonPayload)},
                                td.id as {nameof(DeviceTemplateDetails.Id)},
                                td.key as {nameof(DeviceTemplateDetails.Key)},
                                td.name as {nameof(DeviceTemplateDetails.Name)},
                                td.data_type as {nameof(DeviceTemplateDetails.DataType)},
                                td.detail_id as {nameof(DeviceTemplateDetails.DetailId)},
                                td.enabled as {nameof(DeviceTemplateDetails.Enabled)},
                                td.expression as {nameof(DeviceTemplateDetails.Expression)},
                                td.expression_compile as {nameof(DeviceTemplateDetails.ExpressionCompile)},
                                td.key_type_id as {nameof(DeviceTemplateDetails.KeyTypeId)},
                                td.template_payload_id as {nameof(DeviceTemplateDetails.PayloadId)}
                           from device_templates dt 
                           inner join template_payloads tp on dt.id = tp.device_template_id
                           inner join template_details td on tp.id  = td.template_payload_id
                           where dt.name = ANY(@TemplateNames)";

            var deviceTemplateNames = templates.Select(x => x.Name).ToArray();
            var deviceTemplates = new Dictionary<Guid, DeviceTemplateDb>();
            var deviceTemplatePayloads = new Dictionary<int, DeviceTemplatePayloads>();

            var _ = await connection.QueryAsync<DeviceTemplateDb, DeviceTemplatePayloads, DeviceTemplateDetails, DeviceTemplateDb>(
                query,
                (deviceTemplate, payload, detail) =>
                {
                    deviceTemplate = AddOrGet(deviceTemplates, deviceTemplate, dt => dt.Id, out var _);
                    deviceTemplate.Payloads ??= new List<DeviceTemplatePayloads>();

                    payload = AddOrGet(deviceTemplatePayloads, payload, p => p.PayloadId, out var isNewPayload);
                    payload.Details ??= new List<DeviceTemplateDetails>();
                    if (isNewPayload)
                        deviceTemplate.Payloads.Add(payload);

                    payload.Details.Add(detail);

                    return deviceTemplate;
                },
                new { TemplateNames = deviceTemplateNames }, splitOn: "id,payloadId,id", commandTimeout: 600);

            return deviceTemplates.Values;
        }

        private static T AddOrGet<T, K>(IDictionary<K, T> dictionary, T newEntry, Func<T, K> keySelector, out bool isNew)
        {
            var key = keySelector(newEntry);
            if (dictionary.TryGetValue(key, out var existing))
            {
                isNew = false;
                return existing;
            }
            else
            {
                dictionary[key] = newEntry;
                isNew = true;
                return newEntry;
            }
        }

        private async Task<Guid> AddTemplateAsync(DeviceTemplate template, IDbConnection connection, IDbTransaction transaction)
        {
            var query = $@"insert into {DbName.Table.DEVICE_TEMPLATE}(name, deleted, total_metric, created_by)
                            values(@Name, false, @TotalMetric, @CreatedBy)
                            returning id";
            return await connection.ExecuteScalarAsync<Guid>(query, template, transaction, commandTimeout: 600);
        }

        private async Task UpdateTemplateAsync(int totalMetric, Guid templateId, IDbConnection connection, IDbTransaction transaction)
        {
            var query = $@"update device_templates set updated_utc = @Now, total_metric = @TotalMetric where id = @TemplateId;";
            await connection.ExecuteAsync(query, new { Now = DateTime.UtcNow, TotalMetric = totalMetric, TemplateId = templateId }, transaction, commandTimeout: 600);
        }

        private Task<int> AddTemplatePayloadAsync(TemplatePayload payload, IDbConnection connection, IDbTransaction transaction)
        {
            var query = $@"insert into template_payloads(device_template_id, json_payload)
                            values(@TemplateId, @JsonPayload)
                            returning id";
            return connection.ExecuteScalarAsync<int>(query, payload, transaction, commandTimeout: 600);
        }

        private async Task UpdateTemplatePayloadAsync(DeviceTemplatePayloads payload, IDbConnection connection, IDbTransaction transaction)
        {
            var query = $@"update template_payloads set json_payload = @JsonPayload
                           where id = @PayloadId";
            await connection.ExecuteAsync(query, payload, transaction, commandTimeout: 600);
        }

        private Task<int> AddTemplateDetailAsync(IEnumerable<TemplateDetail> details, IDbConnection connection, IDbTransaction transaction)
        {
            var query = $@"insert into template_details(template_payload_id, key, name, key_type_id, data_type, expression, expression_compile, enabled, detail_id)
                            values (@TemplatePayloadId, @Key, @Name, @KeyTypeId, @DataType, @Expression, @ExpressionCompile, @Enabled, @DetailId);";
            return connection.ExecuteAsync(query, details, transaction, commandTimeout: 600);
        }

        private async Task UpdateTemplateDetailAsync(IEnumerable<DeviceTemplateDetails> details, IDbConnection connection, IDbTransaction transaction)
        {
            var query = $@"update template_details set name = @Name, key_type_id = @KeyTypeId, data_type = @DataType, expression = @Expression, expression_compile = @ExpressionCompile, enabled = @Enabled
                            where id = @Id;";
            await connection.ExecuteAsync(query, details, transaction, commandTimeout: 600);
        }

        private Task<int> AddTemplateBindingAsync(IEnumerable<TemplateBinding> bindings, IDbConnection connection, IDbTransaction transaction)
        {
            var query = $@"insert into template_bindings(device_template_id, key, data_type, default_value)
                            values (@TemplateId, @Key, @DataType, @DefaultValue);";
            return connection.ExecuteAsync(query, bindings, transaction, commandTimeout: 600);
        }

        private async Task<(bool, string)> ValidateExpressionAsync(IDbConnection connection, Guid templateId, TemplateDetail detail, IEnumerable<TemplateDetail> details)
        {
            var expressionValidate = detail.Expression;
            var dataType = detail.DataType;
            // need to make sure the expression is valid and compile
            // *** TODO: NOW VALUE WILL NOT IN VALUE COLUMN ==> now alway true
            if (string.IsNullOrWhiteSpace(expressionValidate))
                return (false, null);

            ICollection<string> metricKeys = new List<string>();
            Match m = Regex.Match(expressionValidate, RegexConstants.DEVICE_TEMPLATE_EXPRESSION_PATTERN, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(10));
            //get metric name in expression
            while (m.Success)
            {
                string idProperty = m.Value.Replace(RegexConstants.EXPRESSION_REFER_OPEN, "").Replace(RegexConstants.EXPRESSION_REFER_CLOSE, "").Trim();
                if (!metricKeys.Contains(idProperty))
                    metricKeys.Add(idProperty);
                m = m.NextMatch();
            }
            var relations = await connection.QueryAsync<ExpressionMetricRelation>(@$"select td.key as MetricKey, td.detail_id as DetailId, td.data_type as DataTypeName
                                                                                            from template_details td
                                                                                            join template_payloads tp on tp.id = td.template_payload_id
                                                                                            where tp.device_template_id = @TemplateId
                                                                                            and td.enabled
                                                                                            and td.key = any(@Keys)", new { TemplateId = templateId, Keys = metricKeys });
            var expressionRelationMetrics = ProcessExpressionMetricRelations(relations, details, metricKeys);


            if (metricKeys.Contains(detail.Key))
            {
                // cannot self-reference in the expression.
                return (false, null);
            }

            var dictionary = new Dictionary<string, object>();
            expressionValidate = BuildExpression(expressionValidate, expressionRelationMetrics, detail, dictionary);
            if (dataType == DataTypeConstants.TYPE_TEXT)
            {
                if (!expressionRelationMetrics.Any(x => expressionValidate.Contains($"request[\"{x.MetricKey.ToString()}\"]")))
                {
                    expressionValidate = expressionValidate.ToJson();
                }
            }
            expressionValidate = expressionValidate.AppendReturn();
            detail.ExpressionCompile = expressionValidate;
            try
            {
                _logger.LogTrace(expressionValidate);
                var value = _dynamicResolver.ResolveInstance("return true;", expressionValidate).OnApply(dictionary);
                if (!string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    if (dataType != DataTypeConstants.TYPE_TEXT)
                        value.ValidateType(dataType);
                    return (true, expressionValidate);
                }
            }
            catch (System.Exception exc)
            {
                _logger.LogError(exc, exc.Message);
            }
            return (false, null);
        }

        private string BuildExpression(string expressionValidate, IEnumerable<ExpressionMetricRelation> expressionRelationMetrics, TemplateDetail detail, IDictionary<string, object> dictionary)
        {
            foreach (var element in expressionRelationMetrics)
            {
                object value = null;
                detail.Expression = detail.Expression.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{element.MetricKey}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"${{{element.DetailId}}}$");
                switch (element.DataTypeName.ToLower())
                {
                    case DataTypeConstants.TYPE_DOUBLE:
                        expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{element.MetricKey}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"Convert.ToDouble(request[{element.MetricKey.ToJson()}])");
                        value = (double)1.0;
                        break;
                    case DataTypeConstants.TYPE_INTEGER:
                        expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{element.MetricKey}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"Convert.ToInt32(request[{element.MetricKey.ToJson()}])");
                        value = 1;
                        break;
                    case DataTypeConstants.TYPE_BOOLEAN:
                        expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{element.MetricKey}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"Convert.ToBoolean(request[{element.MetricKey.ToJson()}])");
                        value = true;
                        break;
                    case DataTypeConstants.TYPE_TEXT:
                        expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{element.MetricKey}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"request[{element.MetricKey.ToJson()}].ToString()");
                        value = "default";
                        break;
                }
                dictionary[element.MetricKey] = value;
            }
            return expressionValidate;
        }

        private IEnumerable<ExpressionMetricRelation> ProcessExpressionMetricRelations(IEnumerable<ExpressionMetricRelation> relations, IEnumerable<TemplateDetail> details, IEnumerable<string> metricKeys)
        {
            var expressionRelationMetrics = new List<ExpressionMetricRelation>();
            if (relations.Any())
            {
                expressionRelationMetrics.AddRange(relations);
            }
            var existingKeys = expressionRelationMetrics.Select(x => x.MetricKey);
            foreach (var dtl in details)
            {
                if (!existingKeys.Contains(dtl.Key) && metricKeys.Contains(dtl.Key))
                {
                    expressionRelationMetrics.Add(new ExpressionMetricRelation { MetricKey = dtl.Key, DataTypeName = dtl.DataType, DetailId = dtl.DetailId });
                }
            }
            return expressionRelationMetrics;
        }

        class MetadataInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        internal class ExpressionMetricRelation
        {
            public string MetricKey { get; set; }
            public Guid DetailId { get; set; } = Guid.NewGuid();
            public string DataTypeName { get; set; }
        }

        internal class DeviceTemplateDetails
        {
            public int Id { get; set; }
            public int PayloadId { get; set; }
            public string Key { get; set; }
            public string Name { get; set; }
            public int KeyTypeId { get; set; }
            public string DataType { get; set; }
            public bool Enabled { get; set; }
            public string Expression { get; set; }
            public string ExpressionCompile { get; set; }
            public Guid DetailId { get; set; }
        }

        internal class DeviceTemplatePayloads
        {
            public int PayloadId { get; set; }
            public string JsonPayload { get; set; }
            public ICollection<DeviceTemplateDetails> Details { get; set; }
        }

        internal class DeviceTemplateDb
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public ICollection<DeviceTemplatePayloads> Payloads { get; set; }
        }
    }
}