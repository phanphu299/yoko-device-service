using System;
using System.Net.Http;
using System.Data.Common;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Dapper;
using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.Constant;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using System.Web;
using AHI.Device.Function.FileParser.Abstraction;
using System.Text.RegularExpressions;
using System.Data;
using System.Linq;
using AHI.Device.Function.Model.SearchModel;
using AHI.Infrastructure.SharedKernel.Model;
using Function.Extension;
using AHI.Infrastructure.SharedKernel.Extension;
using DisplayPropertyName = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.AssetTemplate;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.UserContext.Abstraction;
using AHI.Device.Function.FileParser.Constant;
using System.Globalization;
using AHI.Infrastructure.Repository.Abstraction;
using AHI.Infrastructure.Import.Abstraction;
using AHI.Infrastructure.Service.Tag.Model;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using AHI.Device.Function.FileParser.BaseExcelParser;
namespace AHI.Infrastructure.Repository
{
    public class AssetTemplateRepository : IImportRepository<AssetTemplate>
    {
        private delegate Task<bool> AttributeTypeValidation(AssetTemplateAttribute attribute, NpgsqlConnection connection, IDbTransaction transaction);
        private readonly ErrorType _errorType = ErrorType.DATABASE;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ITenantContext _tenantContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IImportTrackingService _errorService;
        private readonly IDictionary<AssetAttributeType, AttributeTypeValidation> _attributeTypeValidator;
        private readonly IDictionary<string, IntegrateDeviceMetrics> _fetchedIntegrations;
        private readonly NameValidator _templateNameValidator;
        private readonly IDynamicResolver _dynamicResolver;
        private readonly ILoggerAdapter<AssetTemplateRepository> _logger;
        private readonly IUserContext _userContext;
        private readonly Device.Function.FileParser.Abstraction.IParserContext _context;
        private readonly ITagService _tagService;
        private readonly TagValidator _tagValidator;

        public AssetTemplateRepository(IDbConnectionFactory dbConnectionFactory,
            ITenantContext tenantContext,
            IHttpClientFactory factory,
            IDictionary<string, IImportTrackingService> errorHandlers,
            IDynamicResolver dynamicResolver,
            IUserContext userContext,
            Device.Function.FileParser.Abstraction.IParserContext context,
            ITagService tagService,
            ILoggerAdapter<AssetTemplateRepository> logger,
            TagValidator tagValidator)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _tenantContext = tenantContext;
            _httpClientFactory = factory;
            _errorService = errorHandlers[MimeType.EXCEL];
            _dynamicResolver = dynamicResolver;
            _logger = logger;
            _userContext = userContext;
            _context = context;
            _tagService = tagService;
            _attributeTypeValidator = new Dictionary<AssetAttributeType, AttributeTypeValidation>() {
                { AssetAttributeType.STATIC, ValidateStaticAsync },
                { AssetAttributeType.DYNAMIC, ValidateDynamicAsync },
                { AssetAttributeType.RUNTIME, ValidateRuntimeAsync },
                { AssetAttributeType.INTEGRATION, ValidateIntegrationAsync },
                { AssetAttributeType.COMMAND, ValidateCommandAsync },
                { AssetAttributeType.ALIAS, ValidateAliasAsync }
            };

            _fetchedIntegrations = new Dictionary<string, IntegrateDeviceMetrics>();
            _templateNameValidator = new NameValidator(DbName.Table.ASSET_TEMPLATE, "name");
            _templateNameValidator.Seperator = ' ';
            _tagValidator = new TagValidator(_errorService);
        }

        public async Task CommitAsync(IEnumerable<AssetTemplate> source)
        {
            // if any error detected when parsing data in any sheet, discard all file
            if (_errorService.HasError)
                return;

            var dateTimeFormat = _context.GetContextFormat(ContextFormatKey.DATETIMEFORMAT);
            var timezoneOffset = _context.GetContextFormat(ContextFormatKey.DATETIMEOFFSET);
            var offset = TimeSpan.Parse(timezoneOffset);
            bool success = true;
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    foreach (var template in source)
                    {
                        try
                        {
                            if (!await ValidateTemplateAsync(template, connection, transaction))
                            {
                                success &= false;
                                continue;
                            }
                            var insertAssetQuery = $"INSERT INTO {DbName.Table.ASSET_TEMPLATE} (name, created_by) VALUES (@AssetName, @Upn) RETURNING {DbName.Table.ASSET_TEMPLATE}.id";
                            var newAssetTemplateId = await connection.ExecuteScalarAsync<Guid>(insertAssetQuery, new { AssetName = template.Name, Upn = _userContext.Upn }, transaction, commandTimeout: 600);
                            var index = 0;
                            foreach (var attr in template.Attributes)
                            {
                                if (!string.IsNullOrEmpty(attr.Value) && attr.DataType == DataTypeConstants.TYPE_DATETIME)
                                {
                                    var baseTime = DateTime.ParseExact(attr.Value, dateTimeFormat, CultureInfo.InvariantCulture);
                                    var sourceTime = new DateTimeOffset(baseTime, offset);
                                    var utcDate = sourceTime.UtcDateTime;
                                    attr.Value = utcDate.ToString(AHI.Infrastructure.SharedKernel.Extension.Constant.DefaultDateTimeFormat);
                                }
                                index++;
                                var insertAttributeQuery = @"INSERT INTO
                                                                asset_attribute_templates (asset_template_id, name, attribute_type, value, data_type, uom_id, decimal_place, thousand_separator, sequential_number)
                                                            VALUES
                                                                (@AssetTemplateId, @Name, @AttributeType, @Value, @DataType, @Uom, @DecimalPlace, @ThousandSeparator, @Index)
                                                            RETURNING asset_attribute_templates.id";
                                attr.AttributeId = await connection.ExecuteScalarAsync<Guid>(insertAttributeQuery, new
                                {
                                    AssetTemplateId = newAssetTemplateId,
                                    Name = attr.AttributeName,
                                    AttributeType = attr.AttributeType.ToLowerInvariant(),
                                    Value = attr.Value,
                                    DataType = attr.DataType,
                                    Uom = attr.UomId,
                                    DecimalPlace = attr.DecimalPlace,
                                    ThousandSeparator = attr.ThousandSeparator,
                                    Index = index
                                }, transaction, commandTimeout:
                            600);
                                attr.AssetTemplateId = newAssetTemplateId;
                            }

                            foreach (var attr in template.Attributes)
                            {
                                await InsertAttributeRelatedInfoAsync(attr, connection, transaction);
                            }

                            if (!string.IsNullOrEmpty(template.Tags))
                            {
                                if (!_tagValidator.ValidateTags(template.Tags, template, cellIndex: new CellIndex(1, 0)))
                                {
                                    success = false;
                                    continue;
                                }

                                var upsertTag = new UpsertTagCommand
                                {
                                    Upn = _userContext.Upn,
                                    ApplicationId = Guid.Parse(ApplicationInformation.APPLICATION_ID),
                                    IgnoreNotFound = true, // NOTE: DO NOT ADD NEW TAG IF NOT EXIST IN PROJECT
                                    Tags = template.Tags.Split(TagConstants.TAG_IMPORT_EXPORT_SEPARATOR)
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
                                                           INSERT INTO entity_tags (entity_id_uuid, entity_type, tag_id)
                                                           VALUES (@Id, @EntityType, @TagId)";
                                    await connection.ExecuteAsync(updateTagBindingQuery, tagIds.Select(tagId => new { Id = newAssetTemplateId, EntityType = nameof(AssetTemplate), TagId = tagId }), transaction, commandTimeout: 600);
                                }
                            }
                        }
                        catch (DbException e)
                        {
                            _errorService.RegisterError(e.Message, template, null, _errorType);
                            success &= false;
                        }
                    }
                    await (success ? transaction.CommitAsync() : transaction.RollbackAsync());
                }
                await connection.CloseAsync();
            }
        }

        public Task CommitAsync(IEnumerable<AssetTemplate> source, Guid correlationId)
        {
            throw new NotImplementedException();
        }

        private async Task InsertAttributeRelatedInfoAsync(AssetTemplateAttribute attribute, NpgsqlConnection connection, IDbTransaction transaction)
        {
            string query = null;
            object parameter = null;
            if (attribute.Type == AssetAttributeType.DYNAMIC)
            {
                query = @"INSERT INTO asset_attribute_template_dynamics (asset_attribute_template_id, device_template_id, markup_name, metric_key)
                          VALUES (@AttributeId, @TemplateId, @DeviceMarkup, @Metric)";
                parameter = new
                {
                    AttributeId = attribute.AttributeId,
                    TemplateId = attribute.DeviceTemplateId,
                    DeviceMarkup = attribute.DeviceMarkup,
                    Metric = attribute.Metric
                };
            }
            else if (attribute.Type == AssetAttributeType.COMMAND)
            {
                query = @"INSERT INTO asset_attribute_template_commands (asset_attribute_template_id, device_template_id, markup_name, metric_key)
                          VALUES (@AttributeId, @TemplateId, @DeviceMarkup, @Metric)";
                parameter = new
                {
                    AttributeId = attribute.AttributeId,
                    TemplateId = attribute.DeviceTemplateId,
                    DeviceMarkup = attribute.DeviceMarkup,
                    Metric = attribute.Metric
                };
            }
            else if (attribute.Type == AssetAttributeType.INTEGRATION)
            {
                query = @"INSERT INTO asset_attribute_template_integrations (asset_attribute_template_id, integration_markup_name, integration_id, device_markup_name, device_id, metric_key)
                          VALUES (@AttributeId, @IntegrationMarkup, @IntegrationId, @DeviceMarkup, @DeviceId, @Metric)";
                parameter = new
                {
                    AttributeId = attribute.AttributeId,
                    IntegrationMarkup = attribute.ChannelMarkup,
                    IntegrationId = attribute.ChannelId,
                    DeviceMarkup = attribute.DeviceMarkup,
                    DeviceId = attribute.Device,
                    Metric = attribute.Metric
                };
            }
            else if (attribute.Type == AssetAttributeType.RUNTIME)
            {
                var triggerAttributeId = (Guid?)null;
                string expression = null;
                string expressionCompile = null;
                if (attribute.EnabledExpression)
                {
                    triggerAttributeId = await connection.QueryFirstOrDefaultAsync<Guid?>("select id from asset_attribute_templates aat where name = @Name and asset_template_id = @AssetTemplateId  ",
                    new
                    {
                        Name = attribute.TriggerAssetAttribute,
                        AssetTemplateId = attribute.AssetTemplateId,
                    });
                    var (validationResult, expressionResult, expressionCompileResult) = await ValidateExpressionAsync(connection, attribute);
                    if (validationResult)
                    {
                        expression = expressionResult;
                        expressionCompile = expressionCompileResult;
                    }
                }
                query = @"INSERT INTO asset_attribute_template_runtimes (asset_attribute_template_id, enabled_expression, expression, expression_compile, trigger_attribute_id)
                          VALUES (@AttributeId, @EnabledExpression, @Expression, @ExpressionCompile, @TriggerAttributeId)";
                parameter = new
                {
                    AttributeId = attribute.AttributeId,
                    EnabledExpression = attribute.EnabledExpression,
                    Expression = expression,
                    ExpressionCompile = expressionCompile,
                    TriggerAttributeId = triggerAttributeId
                };
            }

            if (query != null)
                await connection.ExecuteAsync(query, parameter, transaction, commandTimeout: 600);
        }

        private async Task<bool> ValidateTemplateAsync(AssetTemplate template, NpgsqlConnection connection, IDbTransaction transaction)
        {
            bool validateAttributes = true;

            foreach (var attr in template.Attributes)
            {
                if (!await ValidateAttributesAsync(attr, connection, transaction))
                    validateAttributes = false;
            }

            var validateMarkups = ValidateDynamicDeviceMarkup(template)
                                && ValidateCommandDeviceMarkup(template)
                                && ValidateIntegrationChannelMarkup(template)
                                && ValidateIntegrationDeviceMarkup(template);

            if (!validateAttributes || !validateMarkups)
                return false;

            ValidateDuplicateAttributeName(template);
            template.Name = await _templateNameValidator.ValidateDuplicateNameAsync(template.Name, connection);

            return true;
        }
        public async Task<(bool, string, string)> ValidateExpressionAsync(IDbConnection connection, AssetTemplateAttribute attribute)
        {
            // need to make sure the expression is valid and compile
            var expression = attribute.Expression.PreProcessExpression();
            var expressionValidate = expression;
            // expression looks like: @(dynamict2Ah) + @(dynamicT3Btu) + @(runtimeout) 
            //get metric in expresstion : {a}*2+{b} => {}
            ICollection<string> assetAttributeNames = new List<string>();
            Match m = Regex.Match(expressionValidate, RegexConstants.PATTERN_EXPRESSION_KEY, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(10));
            //get metric name in expression
            while (m.Success)
            {
                if (!assetAttributeNames.Contains(m.Value))
                    assetAttributeNames.Add(m.Value);
                m = m.NextMatch();
            }
            if (assetAttributeNames.Contains(attribute.AttributeName))
            {
                // cannot self-reference in the expression.
                return (false, null, null);
            }
            var expressionRelationAttributes = await connection.QueryAsync<(Guid, string, string)>("select id, name, data_type from asset_attribute_templates where asset_template_id = @AssetTemplateId",
                new
                {
                    AssetTemplateId = attribute.AssetTemplateId
                });

            var dictionary = new Dictionary<string, object>();
            (expression, expressionValidate) = BuildExpression(expression, expressionValidate, expressionRelationAttributes, dictionary);
            if (attribute.DataType == DataTypeConstants.TYPE_TEXT)
            {
                if (!expressionRelationAttributes.Any(x => expressionValidate.Contains($"request[\"{x.Item1.ToString()}\"]")))
                {
                    expressionValidate = expressionValidate.ToJson();
                }
            }
            expressionValidate = expressionValidate.AppendReturn();
            try
            {
                _logger.LogDebug($"Validate expression");
                var value = _dynamicResolver.ResolveInstance("return true;", expressionValidate).OnApply(dictionary);
                if (!string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    return (true, expression, expressionValidate);
                }
            }
            catch (System.Exception exc)
            {
                _logger.LogError(exc, exc.Message);
            }
            return (false, null, null);
        }

        private (string, string) BuildExpression(string expression, string expressionValidate, IEnumerable<(Guid, string, string)> expressionRelationAttributes, IDictionary<string, object> dictionary)
        {
            foreach (var (elementId, elementName, dataTypeName) in expressionRelationAttributes)
            {
                object value = null;
                string attributeName = elementName;
                if (elementName.Contains(RegexConstants.EXPRESSION_REFER_CLOSE.Trim()))
                {
                    attributeName = $"{elementName.Replace(RegexConstants.EXPRESSION_REFER_CLOSE.Trim(), string.Empty).Trim()}{RegexConstants.EXPRESSION_REFER_CLOSE}";
                }
                expression = expression.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"${{{elementId}}}$", ignoreCase: true, null);
                var lowerDataType = dataTypeName?.ToLower();
                switch (lowerDataType)
                {
                    case DataTypeConstants.TYPE_DOUBLE:
                        expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"Convert.ToDouble(request[\"{elementId}\"])");
                        value = (double)1.0;
                        break;
                    case DataTypeConstants.TYPE_INTEGER:
                        expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"Convert.ToInt32(request[\"{elementId}\"])");
                        value = 1;
                        break;
                    case DataTypeConstants.TYPE_TIMESTAMP:
                        expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"Convert.ToDouble(request[\"{elementId}\"])");
                        value = (double)1;
                        break;
                    case DataTypeConstants.TYPE_BOOLEAN:
                        expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"Convert.ToBoolean(request[\"{elementId}\"])");
                        value = true;
                        break;
                    case DataTypeConstants.TYPE_DATETIME:
                        expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"Convert.ToDateTime(request[\"{elementId}\"])");
                        value = new DateTime(1970, 1, 1);
                        break;
                    case DataTypeConstants.TYPE_TEXT:
                        expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"request[\"{elementId}\"].ToString()");
                        value = "default";
                        break;
                }
                dictionary[elementId.ToString()] = value;
            }
            return (expression, expressionValidate);
        }

        private async Task<bool> ValidateAttributesAsync(AssetTemplateAttribute attribute, NpgsqlConnection connection, IDbTransaction transaction)
        {
            return await _attributeTypeValidator[attribute.Type.Value].Invoke(attribute, connection, transaction)
                 && await ValidateUomAsync(attribute, connection, transaction);
        }

        private Task<bool> ValidateStaticAsync(AssetTemplateAttribute attribute, NpgsqlConnection connection, IDbTransaction transaction)
        {
            return Task.FromResult(TryParseValue(attribute) && TryParseDecimalPlaces(attribute));
        }
        private Task<bool> ValidateAliasAsync(AssetTemplateAttribute attribute, NpgsqlConnection connection, IDbTransaction transaction)
        {
            return Task.FromResult(!string.IsNullOrEmpty(attribute.AttributeName));
        }

        public async Task<bool> ValidateDynamicAsync(AssetTemplateAttribute attribute, NpgsqlConnection connection, IDbTransaction transaction)
        {
            try
            {
                var validateQuery = $@"SELECT
                                        tmp.id AS Template, tmpdtl.key AS Metric, tmpdtl.data_type AS DataType
                                    FROM {DbName.Table.DEVICE_TEMPLATE} tmp
                                    LEFT JOIN template_payloads tmppld ON tmppld.device_template_id = tmp.id
                                    LEFT JOIN
                                        (SELECT * FROM template_details
                                         WHERE template_details.key_type_id IN
                                            (SELECT template_key_types.id FROM template_key_types
                                             WHERE template_key_types.name IN (@MetricType, @AggregationType))
                                         AND template_details.key ILIKE @MetricName) tmpdtl
                                        ON tmpdtl.template_payload_id = tmppld.id
                                    WHERE tmp.name ILIKE @TemplateName and tmpdtl.key is not null";
                // adding checking null to prevent multiple payload, the result can be like this
                /*
                "name"         "id"       "json_payload"
                TVDT_0001       3         "{""key1"": ""abc""}"
                TVDT_0001       2         "{""messageId"": ""thanh-test-001"", ""temperature"": 1700,""humidity"": 200}"

                -- result
                |template                            |metric  |datatype|
                |------------------------------------|--------|--------|
                |85443ae9-223a-46af-a8bb-0e700ff0fbde|        |        | <-- this is wrong, and cause the import is not success
                |85443ae9-223a-46af-a8bb-0e700ff0fbde|humidity|int     | <-- need to get this value

                */
                var result = await connection.QueryFirstOrDefaultAsync<DynamicInfo>(validateQuery, new
                {
                    TemplateName = attribute.DeviceTemplate,
                    MetricName = attribute.Metric,
                    MetricType = "metric",
                    AggregationType = "aggregation"
                }, transaction, commandTimeout: 600);

                if (result?.Template is null)
                {
                    var e = new InvalidOperationException(ValidationMessage.NOT_EXIST);
                    e.Source = nameof(AssetTemplateAttribute.DeviceTemplate);
                    e.Data["validationInfo"] = new Dictionary<string, object>
                    {
                        { "propertyName", DisplayPropertyName.DEVICE_TEMPLATE },
                        { "propertyValue", attribute.DeviceTemplate }
                    };
                    throw e;
                }
                if (result.Metric is null)
                {
                    var e = new InvalidOperationException(ValidationMessage.CHILD_NOT_EXIST);
                    e.Source = nameof(AssetTemplateAttribute.Metric);
                    e.Data["validationInfo"] = new Dictionary<string, object>
                    {
                        { "propertyName", DisplayPropertyName.DEVICE_TEMPLATE },
                        { "propertyValue", attribute.DeviceTemplate },
                        { "childPropertyName", DisplayPropertyName.METRIC },
                        { "childPropertyValue", attribute.Metric }
                    };
                    throw e;
                }

                attribute.DeviceTemplateId = result.Template;
                attribute.DataType = result.DataType;

                return TryParseDecimalPlaces(attribute);
            }
            catch (InvalidOperationException e)
            {
                var propName = e.Source;
                var validationInfo = e.Data["validationInfo"] as Dictionary<string, object>;
                _errorService.RegisterError(e.Message, attribute, propName, _errorType, validationInfo);
                return false;
            }
        }

        public async Task<bool> ValidateCommandAsync(AssetTemplateAttribute attribute, NpgsqlConnection connection, IDbTransaction transaction)
        {
            try
            {
                var validateQuery = $@"SELECT
                                        tmp.id AS Template, tmpb.key AS Metric, tmpb.data_type AS DataType
                                    FROM {DbName.Table.DEVICE_TEMPLATE} tmp
                                    LEFT JOIN template_bindings tmpb
                                        ON tmpb.device_template_id = tmp.id
                                    WHERE tmp.name ILIKE @TemplateName AND tmpb.key ILIKE @MetricName";
                var result = await connection.QueryFirstOrDefaultAsync<DynamicInfo>(validateQuery, new
                {
                    TemplateName = attribute.DeviceTemplate,
                    MetricName = attribute.Metric
                }, transaction, commandTimeout: 600);

                if (result?.Template is null)
                {
                    var e = new InvalidOperationException(ValidationMessage.NOT_EXIST);
                    e.Source = nameof(AssetTemplateAttribute.DeviceTemplate);
                    e.Data["validationInfo"] = new Dictionary<string, object>
                    {
                        { "propertyName", DisplayPropertyName.DEVICE_TEMPLATE },
                        { "propertyValue", attribute.DeviceTemplate }
                    };
                    throw e;
                }
                if (string.IsNullOrEmpty(result?.Metric))
                {
                    var e = new InvalidOperationException(ValidationMessage.CHILD_NOT_EXIST);
                    e.Source = nameof(AssetTemplateAttribute.Metric);
                    e.Data["validationInfo"] = new Dictionary<string, object>
                    {
                        { "propertyName", DisplayPropertyName.DEVICE_TEMPLATE },
                        { "propertyValue", attribute.DeviceTemplate },
                        { "childPropertyName", DisplayPropertyName.METRIC },
                        { "childPropertyValue", attribute.Metric }
                    };
                    throw e;
                }

                attribute.DeviceTemplateId = result.Template;
                attribute.DataType = result.DataType;

                return true;
            }
            catch (InvalidOperationException e)
            {
                var propName = e.Source;
                var validationInfo = e.Data["validationInfo"] as Dictionary<string, object>;
                _errorService.RegisterError(e.Message, attribute, propName, _errorType, validationInfo);
                return false;
            }
        }

        private async Task<bool> ValidateIntegrationAsync(AssetTemplateAttribute attribute, NpgsqlConnection connection, IDbTransaction transaction)
        {
            try
            {
                if (!TryParseDecimalPlaces(attribute))
                    return false;
                var httpClient = _httpClientFactory.CreateClient(ClientNameConstant.BROKER_SERVICE, _tenantContext);
                await FetchIntegrationAsync(attribute, httpClient);
                await FetchIntegrateDeviceMetricAsync(attribute, httpClient);
                attribute.ChannelId = _fetchedIntegrations[attribute.Channel].Id;
                return true;
            }
            catch (InvalidOperationException e)
            {
                var propName = e.Source;
                var validationInfo = e.Data["validationInfo"] as Dictionary<string, object>;
                _errorService.RegisterError(e.Message, attribute, propName, _errorType, validationInfo);
                return false;
            }
        }

        public Task<bool> ValidateRuntimeAsync(AssetTemplateAttribute attribute, NpgsqlConnection connection, IDbTransaction transaction)
        {
            return Task.FromResult(!attribute.EnabledExpression || TryParseDecimalPlaces(attribute));
        }

        public async Task<bool> ValidateUomAsync(AssetTemplateAttribute attribute, NpgsqlConnection connection, IDbTransaction transaction)
        {
            if (attribute.Uom is null)
                return true;

            try
            {
                var uomQuery = @"SELECT id FROM uoms WHERE abbreviation = @Abbreviation";
                attribute.UomId = await connection.QueryFirstAsync<int>(uomQuery, new { Abbreviation = attribute.Uom }, transaction, commandTimeout: 600);
                return true;
            }
            catch (InvalidOperationException)
            {
                _errorService.RegisterError(ValidationMessage.NOT_EXIST, attribute, nameof(AssetTemplateAttribute.Uom), _errorType, new Dictionary<string, object>
                {
                    { "propertyName", DisplayPropertyName.UOM },
                    { "propertyValue", attribute.Uom },
                });
                return false;
            }
        }
        public bool TryParseValue(AssetTemplateAttribute attribute)
        {
            if (string.IsNullOrEmpty(attribute.Value))
                return true;

            var value = attribute.Value;
            var type = attribute.DataType.ToLowerInvariant();
            try
            {
                var dateTimeFormat = _context.GetContextFormat(ContextFormatKey.DATETIMEFORMAT);
                attribute.Value = value.ParseValue(type, dateTimeFormat);
                return true;
            }
            catch (OverflowException)
            {
                var message = ValidationMessage.GENERAL_OUT_OF_RANGE;
                _errorService.RegisterError(message, attribute, nameof(AssetTemplateAttribute.Value), _errorType, new Dictionary<string, object>
                {
                    { "propertyName", DisplayPropertyName.VALUE },
                    { "propertyValue", value },
                    { "propertyType", type }
                });
                return false;
            }
            catch (FormatException)
            {
                var message = ValidationMessage.GENERAL_INVALID_VALUE.Replace("{PropertyName}", DisplayPropertyName.VALUE)
                                                                     .Replace("{PropertyValue}", value);
                _errorService.RegisterError(message, attribute, nameof(AssetTemplateAttribute.Value), _errorType, new Dictionary<string, object>
                {
                    { "propertyName", DisplayPropertyName.VALUE },
                    { "propertyValue", value }
                });
                return false;
            }
            catch (ArgumentOutOfRangeException e)
            {
                var message = ValidationMessage.MAX_LENGTH;
                _errorService.RegisterError(message, attribute, nameof(AssetTemplateAttribute.Value), _errorType, new Dictionary<string, object>
                {
                    { "propertyName", DisplayPropertyName.VALUE },
                    { "maxLength", e.Data["MaxLength"].ToString() }
                });
                return false;
            }
        }
        private bool TryParseDecimalPlaces(AssetTemplateAttribute attribute)
        {
            var decimalPlaces = attribute?.DecimalPlace.ToString();
            var type = attribute.DataType.ToLowerInvariant();
            try
            {
                if (type == DataTypeConstants.TYPE_DOUBLE)
                {
                    if (attribute.DecimalPlace < 0)
                    {
                        var message = ValidationMessage.LESS_THAN_MIN_VALUE;
                        _errorService.RegisterError(message, attribute, nameof(AssetTemplateAttribute.DecimalPlace), _errorType, new Dictionary<string, object>
                        {
                            { "propertyName", DisplayPropertyName.DECIMAL_PLACES },
                            { "propertyValue", decimalPlaces },
                            { "comparisonValue", 0 }
                        });
                        return false;
                    }
                    else if (attribute.DecimalPlace > 15)
                    {
                        var message = ValidationMessage.GREATER_THAN_MAX_VALUE;
                        _errorService.RegisterError(message, attribute, nameof(AssetTemplateAttribute.DecimalPlace), _errorType, new Dictionary<string, object>
                        {
                            { "propertyName", DisplayPropertyName.DECIMAL_PLACES },
                            { "propertyValue", decimalPlaces },
                            { "comparisonValue", 15 }
                        });
                        return false;
                    }
                }
                return true;
            }
            catch (System.Exception e)
            {
                _errorService.RegisterError(e.Message, attribute, nameof(AssetTemplateAttribute.DecimalPlace), _errorType);
                return false;
            }
        }

        public async Task FetchIntegrationAsync(AssetTemplateAttribute attribute, HttpClient httpClient)
        {
            if (_fetchedIntegrations.ContainsKey(attribute.Channel))
                return;

            var filters = FilterIntegrations.GetFilters(attribute.Channel);
            var query = new FilteredSearchQuery(FilteredSearchQuery.LogicalOp.And, filterObjects: filters);
            try
            {
                var response = await httpClient.SearchAsync<IntegrationInfo>($"bkr/integrations/search", query);
                if (response.TotalCount == 0)
                {
                    var e = new InvalidOperationException(ValidationMessage.NOT_EXIST);
                    e.Source = nameof(AssetTemplateAttribute.Channel);
                    e.Data["validationInfo"] = new Dictionary<string, object>
                    {
                        { "propertyName", DisplayPropertyName.CHANNEL },
                        { "propertyValue", attribute.Channel }
                    };
                    throw e;
                }

                var integration = response.Data.First();
                _fetchedIntegrations[attribute.Channel] = new IntegrateDeviceMetrics { Id = Guid.Parse(integration.Id) };
            }
            catch (HttpRequestException)
            {
                var e = new InvalidOperationException(ValidationMessage.NOT_EXIST);
                e.Source = nameof(AssetTemplateAttribute.Channel);
                e.Data["validationInfo"] = new Dictionary<string, object>
                {
                    { "propertyName", DisplayPropertyName.CHANNEL },
                    { "propertyValue", attribute.Channel }
                };
                throw e;
            }
        }

        public async Task FetchIntegrateDeviceMetricAsync(AssetTemplateAttribute attribute, HttpClient httpClient)
        {
            var channel = _fetchedIntegrations[attribute.Channel];
            if (!channel.DeviceMetrics.ContainsKey(attribute.Device))
            {
                var query = HttpUtility.ParseQueryString(string.Empty);
                query["type"] = "metrics";
                query["data"] = attribute.Device;
                var responseMessage = await httpClient.GetAsync($"bkr/integrations/{channel.Id}/fetch?{query.ToString()}");
                if (!responseMessage.IsSuccessStatusCode)
                {
                    var e = new InvalidOperationException(ValidationMessage.NOT_EXIST);
                    e.Source = nameof(AssetTemplateAttribute.Device);
                    e.Data["validationInfo"] = new Dictionary<string, object>
                    {
                        { "propertyName", DisplayPropertyName.DEVICE },
                        { "propertyValue", attribute.Device }
                    };
                    throw e;
                }

                var message = await responseMessage.Content.ReadAsByteArrayAsync();
                var response = message.Deserialize<BaseSearchResponse<IntegrationInfo>>();

                channel.AddDeviceMetrics(attribute.Device, response.Data.Select(x => x.Name));
            }
            if (!channel.ContainsDeviceMetric(attribute.Device, attribute.Metric))
            {
                var e = new InvalidOperationException(ValidationMessage.CHILD_NOT_EXIST);
                e.Source = nameof(AssetTemplateAttribute.Metric);
                e.Data["validationInfo"] = new Dictionary<string, object>
                {
                    { "propertyName", DisplayPropertyName.DEVICE },
                    { "propertyValue", attribute.Device },
                    { "childPropertyName", DisplayPropertyName.METRIC },
                    { "childPropertyValue", attribute.Metric }
                };
                throw e;
            }
        }

        public void ValidateDuplicateAttributeName(AssetTemplate template)
        {
            var duplicates = new Dictionary<string, ICollection<int>>();
            var trailingRegex = new Regex(@"( copy)+$", RegexOptions.IgnoreCase);
            foreach (var attribute in template.Attributes)
            {
                var noSuffixName = trailingRegex.Replace(attribute.AttributeName, string.Empty).ToLowerInvariant();
                if (!duplicates.TryGetValue(noSuffixName, out var duplicate))
                {
                    duplicates[noSuffixName] = duplicate = new List<int>();
                }
                var offset = Regex.Matches(trailingRegex.Match(attribute.AttributeName).Value, "copy", RegexOptions.IgnoreCase).Count;
                attribute.AttributeName = NameValidator.AppendCopy(attribute.AttributeName, " ", duplicate, offset);
            }
        }

        public bool ValidateDynamicDeviceMarkup(AssetTemplate template)
        {
            var dynamicAttributes = template.Attributes.Where(attribute => attribute.Type == AssetAttributeType.DYNAMIC);

            Func<AssetTemplateAttribute, string> deviceTemplateSelector = attribute => attribute.DeviceTemplate.ToLowerInvariant();
            Func<AssetTemplateAttribute, string> markupDeviceSelector = attribute => attribute.DeviceMarkup.ToLowerInvariant();

            var errorMessage = ValidationMessage.MARKUP_DUPLICATED;
            Action<AssetTemplateAttribute, string, string> markupErrorHandler =
                (attribute, key, value) => _errorService.RegisterError(errorMessage, attribute, nameof(AssetTemplateAttribute.DeviceMarkup), validationInfo: new Dictionary<string, object>
                {
                    { "markupName", DisplayPropertyName.MARKUP_DEVICE },
                    { "propertyName", DisplayPropertyName.DEVICE_TEMPLATE }
                });

            // 1 device markup can belong to 1 device template only, 1 device template can have multiple device markups => markup - template is many - 1
            return ValidateManyToOne(dynamicAttributes, markupDeviceSelector, deviceTemplateSelector, markupErrorHandler);
        }

        public bool ValidateCommandDeviceMarkup(AssetTemplate template)
        {
            var commandAttributes = template.Attributes.Where(attribute => attribute.Type == AssetAttributeType.COMMAND);

            Func<AssetTemplateAttribute, string> deviceTemplateSelector = attribute => attribute.DeviceTemplate.ToLowerInvariant();
            Func<AssetTemplateAttribute, string> markupDeviceSelector = attribute => attribute.DeviceMarkup.ToLowerInvariant();

            var errorMessage = ValidationMessage.MARKUP_DUPLICATED;
            Action<AssetTemplateAttribute, string, string> markupErrorHandler =
                (attribute, key, value) => _errorService.RegisterError(errorMessage, attribute, nameof(AssetTemplateAttribute.DeviceMarkup), validationInfo: new Dictionary<string, object>
                {
                    { "markupName", DisplayPropertyName.MARKUP_DEVICE },
                    { "propertyName", DisplayPropertyName.DEVICE_TEMPLATE }
                });

            // 1 device markup can belong to 1 device template only, 1 device template can have multiple device markups => markup - template is many - 1
            return ValidateManyToOne(commandAttributes, markupDeviceSelector, deviceTemplateSelector, markupErrorHandler);
        }
        // private bool ValidateRuntimeAssetMarkup(AssetTemplate template)
        // {
        //     var dynamicAttributes = template.Attributes.Where(attribute => attribute.Type == AssetAttributeType.RUNTIME);

        //     Func<AssetTemplateAttribute, string> assetTemplateMarkupSelector = attribute => attribute.TriggerAssetTemplate?.ToLowerInvariant();
        //     Func<AssetTemplateAttribute, string> assetTemplateMarkupValueSelector = attribute => attribute.TriggerAssetMarkup?.ToLowerInvariant();

        //     var errorMessage = $"{DisplayPropertyName.MARKUP_TRIGGER_ASSET} is used on another {DisplayPropertyName.ASSET_TEMPLATE}.";
        //     Action<AssetTemplateAttribute, string, string> markupErrorHandler = (attribute, key, value) => _errorService.RegisterError(errorMessage, attribute, nameof(AssetTemplateAttribute.TriggerAssetMarkup));

        //     // 1 device markup can belong to 1 device template only, 1 device template can have multiple device markups => markup - template is many - 1
        //     return ValidateManyToOne(dynamicAttributes, assetTemplateMarkupSelector, assetTemplateMarkupValueSelector, markupErrorHandler);
        // }

        public bool ValidateIntegrationChannelMarkup(AssetTemplate template)
        {
            var integrationAttributes = template.Attributes.Where(attribute => attribute.Type == AssetAttributeType.INTEGRATION);

            Func<AssetTemplateAttribute, string> channelSelector = attribute => attribute.Channel.ToLowerInvariant();
            Func<AssetTemplateAttribute, string> markupChannelSelector = attribute => attribute.ChannelMarkup.ToLowerInvariant();

            var channelErrorMessage = ValidationMessage.ALREADY_HAVE_MARKUP;
            Action<AssetTemplateAttribute, string, string> channelErrorHandler =
                (attribute, key, value) => _errorService.RegisterError(channelErrorMessage, attribute, nameof(AssetTemplateAttribute.Channel), validationInfo: new Dictionary<string, object>
                {
                    { "markupName", DisplayPropertyName.MARKUP_CHANNEL },
                    { "propertyName", DisplayPropertyName.CHANNEL }
                });

            var markupErrorMessage = ValidationMessage.MARKUP_USED;
            Action<AssetTemplateAttribute, string, string> markupErrorHandler =
                (attribute, key, value) => _errorService.RegisterError(markupErrorMessage, attribute, nameof(AssetTemplateAttribute.ChannelMarkup), validationInfo: new Dictionary<string, object>
                {
                    { "markupName", DisplayPropertyName.MARKUP_CHANNEL },
                    { "propertyName", DisplayPropertyName.CHANNEL }
                });

            return ValidateOneToOne(integrationAttributes, channelSelector, markupChannelSelector, channelErrorHandler, markupErrorHandler);
        }

        public bool ValidateIntegrationDeviceMarkup(AssetTemplate template)
        {
            // validate by each channel
            var channelGroups = template.Attributes.Where(attribute => attribute.Type == AssetAttributeType.INTEGRATION)
                                                   .GroupBy(attribute => attribute.Channel.ToLowerInvariant());

            Func<AssetTemplateAttribute, string> deviceSelector = attribute => attribute.Device.ToLowerInvariant();
            Func<AssetTemplateAttribute, string> markupDeviceSelector = attribute => attribute.DeviceMarkup.ToLowerInvariant();

            var deviceErrorMessage = ValidationMessage.ALREADY_HAVE_MARKUP;
            Action<AssetTemplateAttribute, string, string> deviceErrorHandler =
                (attribute, key, value) => _errorService.RegisterError(deviceErrorMessage, attribute, nameof(AssetTemplateAttribute.Device), validationInfo: new Dictionary<string, object>
                {
                    { "markupName", DisplayPropertyName.MARKUP_DEVICE },
                    { "propertyName", DisplayPropertyName.DEVICE }
                });

            var markupErrorMessage = ValidationMessage.MARKUP_USED;
            Action<AssetTemplateAttribute, string, string> markupErrorHandler =
                (attribute, key, value) => _errorService.RegisterError(markupErrorMessage, attribute, nameof(AssetTemplateAttribute.DeviceMarkup), validationInfo: new Dictionary<string, object>
                {
                    { "markupName", DisplayPropertyName.MARKUP_DEVICE },
                    { "propertyName", DisplayPropertyName.DEVICE }
                });

            return channelGroups.Aggregate(true, (isValid, channelAttributes) =>
            {
                if (!ValidateOneToOne(channelAttributes, deviceSelector, markupDeviceSelector, deviceErrorHandler, markupErrorHandler))
                    isValid = false;
                return isValid;
            });
        }

        // Validate TKey - TValue is many - 1
        private bool ValidateManyToOne<T, TKey, TValue>(
            IEnumerable<T> entities,
            Func<T, TKey> keySelector,
            Func<T, TValue> valueSelector,
            Action<T, TKey, TValue> keyErrorHandler)
        {
            var validValues = new Dictionary<TKey, TValue>();

            return entities.Aggregate(true, (isValid, entity) =>
            {
                var key = keySelector.Invoke(entity);
                var value = valueSelector.Invoke(entity);
                if (key == null && value == null)
                {
                    return true;
                }
                if (!validValues.ContainsKey(key))
                {
                    validValues[key] = value;
                }
                else if (!validValues[key].Equals(value))
                {
                    isValid = false;
                    keyErrorHandler.Invoke(entity, key, value);
                }

                return isValid;
            });
        }

        // Validate TKey - TValue is 1 - 1
        private bool ValidateOneToOne<T, TKey, TValue>(
            IEnumerable<T> entities,
            Func<T, TKey> keySelector,
            Func<T, TValue> valueSelector,
            Action<T, TKey, TValue> keyErrorHandler,
            Action<T, TKey, TValue> valueErrorHandler)
        {
            var validMatched = new HashSet<KeyValuePair<TKey, TValue>>();

            return entities.Aggregate(true, (isValid, entity) =>
            {
                var current = KeyValuePair.Create(keySelector.Invoke(entity), valueSelector.Invoke(entity));

                // If current pair already existed as valid pair, skip.
                if (validMatched.Contains(current))
                    return isValid;

                // Check if either the current pair has duplications of key, or duplications of value (violate 1 to 1 rule).
                // If there are no duplication, add current pair as a new valid pair.
                var invalidEntities = validMatched.Where(previousMatch => previousMatch.Key.Equals(current.Key));
                if (!invalidEntities.Any())
                    invalidEntities = validMatched.Where(previousMatch => previousMatch.Value.Equals(current.Value));
                if (!invalidEntities.Any())
                {
                    validMatched.Add(current);
                    return isValid;
                }

                foreach (var existedEntity in invalidEntities)
                {
                    isValid = false;
                    if (existedEntity.Key.Equals(current.Key))
                    {
                        keyErrorHandler.Invoke(entity, current.Key, current.Value);
                    }
                    else if (existedEntity.Value.Equals(current.Value))
                    {
                        valueErrorHandler.Invoke(entity, current.Key, current.Value);
                    }
                }
                return isValid;
            });
        }

        class DynamicInfo
        {
            public Guid Template { get; set; }
            public string Metric { get; set; }
            public string DataType { get; set; }
        }

        class AssetTemplateTrigger
        {
            public Guid? AttributeId { get; set; }
        }

        class IntegrationInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        class IntegrateDeviceMetrics
        {
            public Guid Id { get; set; }
            public IDictionary<string, HashSet<string>> DeviceMetrics { get; set; } = new Dictionary<string, HashSet<string>>();

            public bool ContainsDeviceMetric(string device, string metric)
            {
                return DeviceMetrics.ContainsKey(device) && DeviceMetrics[device].Contains(metric);
            }

            public void AddDeviceMetrics(string device, IEnumerable<string> addMetrics)
            {
                if (DeviceMetrics.ContainsKey(device))
                    DeviceMetrics[device].UnionWith(addMetrics);
                else
                    DeviceMetrics[device] = new HashSet<string>(addMetrics);
            }
        }
    }
}
