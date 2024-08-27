using AHI.Device.Function.FileParser.Constant;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Device.Function.FileParser.Abstraction;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.FileImport.Abstraction;
using Microsoft.Azure.WebJobs;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using System.Linq;
using AHI.Device.Function.FileParser.ErrorTracking.Model;
using AHI.Device.Function.Model.ImportModel.Attribute;
using Function.Extension;
using Npgsql;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Device.Function.Constant;
using System.Net.Http;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Device.Function.Model.SearchModel;
using System.Globalization;
using AHI.Device.Function.Model.ImportModel;
using static AHI.Device.Function.Constant.ErrorMessage;
using ErrorDetail = AHI.Device.Function.Model.ImportModel.Attribute.ErrorDetail;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.SharedKernel.Model;
using System.Text.RegularExpressions;
using AHI.Infrastructure.Interceptor.Abstraction;

namespace AHI.Device.Function.Service
{
    public class TemplateAttributeParserService : ITemplateAttributeParserService
    {
        private readonly IImportNotificationService _notification;
        private readonly IDictionary<string, IImportTrackingService> _errorHandlers;
        private readonly IStorageService _storageService;
        private readonly ILoggerAdapter<FileImportService> _logger;
        private IImportTrackingService _errorService;
        private readonly IAssetAttributeTemplateImportService _importService;
        private readonly IParserContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISystemContext _systemContext;
        private readonly IDynamicResolver _dynamicResolver;

        public TemplateAttributeParserService(IImportNotificationService notification,
                                 IDictionary<string, IImportTrackingService> errorHandlers,
                                 IStorageService storageService,
                                 ILoggerAdapter<FileImportService> logger,
                                 IAssetAttributeTemplateImportService importService,
                                 IParserContext context,
                                 ITenantContext tenantContext,
                                 IConfiguration configuration,
                                 IHttpClientFactory httpClientFactory,
                                 ISystemContext systemContext,
                                 IDynamicResolver dynamicResolver)
        {
            _notification = notification;
            _errorHandlers = errorHandlers;
            _storageService = storageService;
            _logger = logger;
            _importService = importService;
            _context = context;
            _tenantContext = tenantContext;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _systemContext = systemContext;
            _dynamicResolver = dynamicResolver;
        }

        public async Task<ImportAttributeResponse> ParseAsync(AssetTemplateAttributeMessage message, Guid activityId, ExecutionContext context)
        {
            var response = new ImportAttributeResponse();
            var attributes = new List<AttributeTemplate>();
            var mimeType = EntityFileMapping.GetMimeType(message.ObjectType);
            _errorService = _errorHandlers[mimeType];

            _notification.Upn = message.Upn;
            _notification.ActivityId = activityId;
            _notification.ObjectType = message.ObjectType;
            _notification.NotificationType = ActionType.Import;
            _context.SetExecutionContext(context, ParseAction.IMPORT);
            _context.SetContextFormat(ContextFormatKey.DATETIMEFORMAT, message.DateTimeFormat);
            _context.SetContextFormat(ContextFormatKey.DATETIMEOFFSET, DateTimeExtensions.ToValidOffset(message.DateTimeOffset));
            // send signalR starting import
            await _notification.SendStartNotifyAsync(1);
            var file = StringExtension.RemoveFileToken(message.FileName);
            _errorService.File = file;
            using (var stream = new System.IO.MemoryStream())
            {
                await DownloadParseFileAsync(file, stream);
                if (stream.CanRead)
                {
                    try
                    {
                        var fileHandler = _importService.GetFileHandler();
                        var attributesParsed = fileHandler.Handle(stream);

                        var validAttributes = await ParseAttributeTemplateDetailAsync(message.TemplateId, attributesParsed, message.UnsavedAttributes);
                        attributes.AddRange(validAttributes);
                    }
                    catch (Exception ex)
                    {
                        _errorService.RegisterError(ex.Message, ErrorType.UNDEFINED);
                        _logger.LogError(ex, ex.Message);
                    }
                }
            }

            response.Attributes = attributes;
            var errors = _errorService.FileErrors[message.FileName].ToList();

            if (errors != null && errors.Any())
            {
                var trackErrors = new List<ErrorDetail>();
                foreach (var error in errors.OfType<ExcelTrackError>())
                {
                    if (trackErrors.Any(x => x.Column == error.Column && x.Row == error.Row))
                        continue;

                    var errorMessage = new ErrorDetail()
                    {
                        Column = error.Column,
                        Detail = error.Message,
                        Row = error.Row,
                        Type = error.Type,
                        ColumnName = GetPropertyError(ErrorProperty.ERROR_PROPERTY_NAME, error.ValidationInfo),
                        ColumnValue = GetPropertyError(ErrorProperty.ERROR_PROPERTY_VALUE, error.ValidationInfo),
                    };
                    trackErrors.Add(errorMessage);
                }
                response.Errors = trackErrors;
            }
            else
            {
                await _notification.SendFinishImportNotifyAsync(ActionStatus.Success, (attributes.Count, 0));
            }
            return response;
        }

        private string GetPropertyError(string key, IDictionary<string, object> validation)
        {
            return validation != null && validation.ContainsKey(key) ? validation[key]?.ToString() : string.Empty;
        }

        private async Task DownloadParseFileAsync(string fileName, System.IO.Stream outputStream)
        {
            try
            {
                await _storageService.DownloadFileToStreamAsync(fileName, outputStream);
            }
            catch
            {
                outputStream.Dispose();
                _errorService.RegisterError(ValidationMessage.GET_FILE_FAILED, ErrorType.UNDEFINED);
            }
        }

        private async Task<IEnumerable<AttributeTemplate>> ParseAttributeTemplateDetailAsync(
            string templateId,
            IEnumerable<AttributeTemplate> attributes,
            IEnumerable<UnsaveAttribute> unSaveAttributes)
        {
            var validAttributes = new List<AttributeTemplate>();
            var existingAttributes = unSaveAttributes.Select(x => new AttributeSimple(x.Id, x.Name, x.AttributeType, x.DataType, x.UpdatedUtc));
            var attributeDeviceMarkups = new List<TemplateMarkupDevice>();
            var existingIntegrationAttributes = unSaveAttributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_INTEGRATION)
                                                                .Select(x => new AttributeTemplate(x.Id, x.AttributeType, x.IntegrationId, x.IntegrationMarkupName, x.DeviceMarkupName, x.DeviceId))
                                                                .ToList();

            using (var connection = GetDbConnection())
            {
                await connection.OpenAsync();

                var deviceTemplateIds = unSaveAttributes.Where(x => x.DeviceTemplateId.HasValue).Select(x => x.DeviceTemplateId.Value);
                var deviceTemplates = await FetchDeviceTemplatesAsync(deviceTemplateIds, connection);
                var markupAttrs = unSaveAttributes.Where(x => AttributeTypeConstants.ATTRIBUTES_HAVE_MARKUP.Contains(x.AttributeType))
                                                  .Select(x => new TemplateMarkupDevice(x.Id, x.AttributeType, deviceTemplates.GetValueOrDefault(x.DeviceTemplateId)?.ToString(), x.MarkupName, x.MetricKey));
                attributeDeviceMarkups.AddRange(markupAttrs);
                var attributesUsingTemplate = await FetchAttributeUsingTemplateAsync(templateId, connection);
                IEnumerable<AttributeTemplate> nonDuplicateAttributeTemplates;
                nonDuplicateAttributeTemplates = HandleDuplicateAttribute(attributesUsingTemplate, attributes, existingAttributes);
                foreach (var attr in nonDuplicateAttributeTemplates)
                {
                    await GetUomAsync(attr, connection);
                    ValidateDataType(attr);

                    switch (attr.AttributeType.ToLower())
                    {
                        case AttributeTypeConstants.TYPE_STATIC:
                            await ValidateStaticAsync(attr);
                            break;
                        case AttributeTypeConstants.TYPE_COMMAND:
                            await ValidateCommandAsync(attr, validAttributes, attributeDeviceMarkups, connection);
                            break;
                        case AttributeTypeConstants.TYPE_DYNAMIC:
                            await ValidateDynamicAsync(attr, validAttributes, attributeDeviceMarkups, connection);
                            break;
                        case AttributeTypeConstants.TYPE_INTEGRATION:
                            await ValidateIntegrationAsync(attr, existingIntegrationAttributes);
                            break;
                        case AttributeTypeConstants.TYPE_RUNTIME:
                            await ValidateRuntimeAsync(attr, attributes, existingAttributes);
                            break;
                    }
                    ValidateDecimalPlace(attr);
                    ValidateThousandSeparator(attr);

                    validAttributes.Add(attr);
                }
                connection.Close();
            }
            return validAttributes;
        }

        private IEnumerable<AttributeTemplate> HandleDuplicateAttribute(IEnumerable<AttributeSimple> attributesUsingTemplate, IEnumerable<AttributeTemplate> attributes, IEnumerable<AttributeSimple> existingAttributes)
        {
            var nonDuplicateAttributeTemplates = new List<AttributeTemplate>();
            foreach (var attr in attributes)
            {
                var existingAttribute = existingAttributes.FirstOrDefault(x => string.Equals(x.AttributeName, attr.AttributeName, StringComparison.InvariantCultureIgnoreCase));
                if (existingAttribute != null)
                {
                    if (!string.Equals(existingAttribute.AttributeType, attr.AttributeType, StringComparison.InvariantCultureIgnoreCase)
                    || existingAttribute.TemplateAttributeId != null)
                    {
                        _errorService.RegisterError(ParseValidation.PARSER_DUPLICATED_ATTRIBUTE_NAME, attr, nameof(AssetAttribute.AttributeName), validationInfo: new Dictionary<string, object>
                        {
                            { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.ATTRIBUTE_NAME },
                            { ErrorProperty.ERROR_PROPERTY_VALUE, attr.AttributeName }
                        });
                        continue;
                    }
                    attr.AttributeId = existingAttribute.Id;
                    attr.UpdatedUtc = existingAttribute.UpdatedUtc;
                }

                if (attributesUsingTemplate.Any(x => string.Equals(x.AttributeName, attr.AttributeName) && string.Equals(x.AttributeType, attr.AttributeType)))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_DUPLICATED_ATTRIBUTE_NAME, attr, nameof(AttributeTemplate.AttributeName), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.ATTRIBUTE_NAME },
                        { ErrorProperty.ERROR_PROPERTY_VALUE, attr.AttributeName }
                    });
                }
                nonDuplicateAttributeTemplates.Add(attr);
            }
            return nonDuplicateAttributeTemplates;
        }

        private async Task<IDictionary<Guid, string>> FetchDeviceTemplatesAsync(IEnumerable<Guid> deviceTemplateIds, NpgsqlConnection connection)
        {
            var query = @"select id, name
                         from device_templates
                         where id = ANY(@DeviceTemplateIds)";
            var parameters = new DynamicParameters();
            parameters.Add("@DeviceTemplateIds", deviceTemplateIds.ToList());
            return (await connection.QueryAsync<(Guid, string)>(query, parameters)).ToDictionary(x => x.Item1, x => x.Item2);
        }

        private async Task<IEnumerable<AttributeSimple>> FetchAttributeUsingTemplateAsync(string assetTemplateId, NpgsqlConnection connection)
        {
            if (string.IsNullOrEmpty(assetTemplateId))
                return Array.Empty<AttributeSimple>();
            var query = @"select
                            aa.name as AttributeName,
                            aa.attribute_type as AttributeType
                         from asset_attributes aa
                         join assets a on aa.asset_id = a.id 
                         where a.asset_template_id = @AssetTemplateId";
            return await connection.QueryAsync<AttributeSimple>(query, new { AssetTemplateId = Guid.Parse(assetTemplateId) });
        }

        private void ValidateDataType(AttributeTemplate attribute)
        {
            if (attribute.Type == AssetAttributeType.RUNTIME || attribute.Type == AssetAttributeType.INTEGRATION)
            {
                var validData = DataTypeExtensions.IsDataTypeForTemplateAttribute(attribute.DataType);
                if (validData)
                    return;
                else
                {
                    _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AttributeTemplate.DataType), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.DATA_TYPE }
                    });
                    attribute.DataType = DataTypeConstants.TYPE_TEXT;
                }
            }
        }

        private void ValidateDecimalPlace(AttributeTemplate attr)
        {
            try
            {
                if (!AttributeTypeConstants.ATTRIBUTES_HAVE_DATA_TYPE.Contains(attr.AttributeType.ToLower()) || attr.DataType != DataTypeConstants.TYPE_DOUBLE)
                {
                    attr.DecimalPlace = FormatDefaultConstants.ATTRIBUTE_DECIMAL_PLACES_MAX.ToString();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(attr.DecimalPlace))
                {
                    var parseData = int.TryParse(attr.DecimalPlace, out int result);

                    if (parseData && result >= FormatDefaultConstants.ATTRIBUTE_DECIMAL_PLACES_MIN && result <= FormatDefaultConstants.ATTRIBUTE_DECIMAL_PLACES_MAX)
                        return;
                    else
                        throw new FormatException();
                }
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attr, nameof(AttributeTemplate.DecimalPlace), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.DECIMAL_PLACE }
                });
                attr.DecimalPlace = string.Empty;
            }
        }

        private void ValidateThousandSeparator(AttributeTemplate attr)
        {
            try
            {
                if (!AttributeTypeConstants.ATTRIBUTES_HAVE_DATA_TYPE.Contains(attr.AttributeType.ToLower())
                    || (attr.DataType != DataTypeConstants.TYPE_DOUBLE && attr.DataType != DataTypeConstants.TYPE_INTEGER))
                {
                    attr.ThousandSeparator = FormatDefaultConstants.ATTRIBUTE_THOUSAND_SEPARATOR_DEFAULT;
                    return;
                }

                if (string.IsNullOrWhiteSpace(attr.ThousandSeparator))
                    throw new FormatException();

                var parseData = bool.TryParse(attr.ThousandSeparator, out _);

                if (!parseData)
                    throw new FormatException();
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attr, nameof(AttributeTemplate.ThousandSeparator), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.THOUSAND_SEPARATOR }
                });
                attr.ThousandSeparator = FormatDefaultConstants.ATTRIBUTE_THOUSAND_SEPARATOR_DEFAULT;
            }
        }

        private async Task ValidateStaticAsync(AttributeTemplate attribute)
        {
            try
            {
                if (string.IsNullOrEmpty(attribute.Value))
                    return;

                await ValidateColumnDataAsync(attribute.Value, null, true);

                var dateTimeFormat = _context.GetContextFormat(ContextFormatKey.DATETIMEFORMAT);
                var timezoneOffset = _context.GetContextFormat(ContextFormatKey.DATETIMEOFFSET);
                var type = attribute.DataType.ToLowerInvariant();

                var data = attribute.Value.ParseValue(type, dateTimeFormat);

                if (type == DataTypeConstants.TYPE_DATETIME)
                {
                    var baseTime = DateTime.ParseExact(data, dateTimeFormat, CultureInfo.InvariantCulture);
                    var offset = TimeSpan.Parse(timezoneOffset);
                    var sourceTime = new DateTimeOffset(baseTime, offset);
                    var utcDate = sourceTime.UtcDateTime;
                    attribute.Value = utcDate.ToString(AHI.Infrastructure.SharedKernel.Extension.Constant.DefaultDateTimeFormat);
                }
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AttributeTemplate.Value), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.VALUE }
                });
                attribute.Value = null;
            }
        }

        private async Task ValidateRuntimeAsync(AttributeTemplate currentAttribute, IEnumerable<AttributeTemplate> attributes, IEnumerable<AttributeSimple> existingAttributes)
        {
            if (!ValidateEnableExpression(currentAttribute))
                return;

            var attrs = new List<AttributeSimple>();

            if (string.IsNullOrWhiteSpace(currentAttribute.Expression))
            {
                _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, currentAttribute, nameof(AttributeTemplate.Expression), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.EXPRESSION }
                });
            }
            else
            {
                attrs.AddRange(attributes.Select(x => new AttributeSimple((Guid)x.AttributeId, x.AttributeName, x.AttributeType, x.DataType)));
                var attributeIds = attributes.Select(x => x.AttributeId);
                attrs.AddRange(existingAttributes.Where(x => !attributeIds.Contains(x.Id)));

                ValidateExpression(currentAttribute, attrs);
            }

            if (string.IsNullOrEmpty(currentAttribute.TriggerAssetAttribute))
                return;

            await ValidateTriggerAttributeAsync(currentAttribute, attrs);
        }

        private void ValidateExpression(AttributeTemplate currentAttribute, IEnumerable<AttributeSimple> attrs)
        {
            var expression = currentAttribute.Expression.PreProcessExpression();
            var expressionValidate = expression;
            var dictionary = new Dictionary<string, object>();
            foreach (var attr in attrs)
            {
                ProcessAttributeExpression(attr, dictionary, ref expression, ref expressionValidate);
            }

            if (attrs.Any(x => expression.Contains(x.Id.ToString()) && !expression.Contains($"${{{x.Id}}}$")) ||
                expression.Contains(currentAttribute.AttributeId?.ToString()))
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, currentAttribute, nameof(AssetAttribute.Expression), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.EXPRESSION }
                });
                currentAttribute.Expression = null;
                return;
            }

            if (attrs.Any(x => expressionValidate.Contains(x.Id.ToString()) &&
                ((x.AttributeType == AttributeTypeConstants.TYPE_COMMAND) || x.Id == currentAttribute.AttributeId)))
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, currentAttribute, nameof(AttributeTemplate.Expression), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.EXPRESSION }
                });
                currentAttribute.Expression = null;
                return;
            }

            if (currentAttribute.DataType == DataTypeConstants.TYPE_TEXT
                && !attrs.Any(x => expressionValidate.Contains($"request[\"{x.Id}\"]")))
            {
                expressionValidate = expressionValidate.ToJson();
            }

            expressionValidate = expressionValidate.AppendReturn();

            try
            {
                if (expressionValidate.Contains(RegexConstants.EXPRESSION_REFER_OPEN) && expressionValidate.Contains(RegexConstants.EXPRESSION_REFER_CLOSE))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, currentAttribute, nameof(AttributeTemplate.Expression), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.EXPRESSION }
                    });
                    currentAttribute.Expression = null;
                    return;
                }

                var value = _dynamicResolver.ResolveInstance("return true;", expressionValidate).OnApply(dictionary);

                if (!string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    StringExtension.ParseValue(value.ToString(), currentAttribute.DataType, null);
                    currentAttribute.Expression = expression;
                }
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, currentAttribute, nameof(AttributeTemplate.Expression), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.EXPRESSION }
                });
                currentAttribute.Expression = null;
            }
        }

        private void ProcessAttributeExpression(AttributeSimple attr, Dictionary<string, object> dictionary,
                                                ref string expression, ref string expressionValidate)
        {
            if (string.IsNullOrEmpty(attr.DataType))
                return;

            object draftValue = null;
            string attributeName = attr.AttributeName;
            if (attr.AttributeName.Contains(RegexConstants.EXPRESSION_REFER_CLOSE.Trim()))
            {
                attributeName = $"{attr.AttributeName.Replace(RegexConstants.EXPRESSION_REFER_CLOSE.Trim(), string.Empty).Trim()}{RegexConstants.EXPRESSION_REFER_CLOSE}";
            }
            expression = expression.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"${{{attr.Id}}}$", ignoreCase: true, null);

            switch (attr.DataType?.ToLower())
            {
                case DataTypeConstants.TYPE_INTEGER:
                    expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"Convert.ToInt32(request[\"{attr.Id}\"])", ignoreCase: true, null);
                    draftValue = 1;
                    break;
                case DataTypeConstants.TYPE_DOUBLE:
                    expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"Convert.ToDouble(request[\"{attr.Id}\"])", ignoreCase: true, null);
                    draftValue = (double)1.0;
                    break;
                case DataTypeConstants.TYPE_BOOLEAN:
                    expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"Convert.ToBoolean(request[\"{attr.Id}\"])", ignoreCase: true, null);
                    draftValue = true;
                    break;
                case DataTypeConstants.TYPE_DATETIME:
                    expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"Convert.ToDateTime(request[\"{attr.Id}\"])", ignoreCase: true, null);
                    draftValue = new DateTime(1970, 1, 1);
                    break;
                case DataTypeConstants.TYPE_TEXT:
                    expressionValidate = expressionValidate.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"request[\"{attr.Id}\"].ToString()", ignoreCase: true, null);
                    draftValue = "default";
                    break;
            }
            dictionary[attr.Id.ToString()] = draftValue;
        }

        private async Task ValidateTriggerAttributeAsync(AttributeTemplate currentAttribute, IEnumerable<AttributeSimple> attributes)
        {
            try
            {
                await ValidateColumnDataAsync(currentAttribute.TriggerAssetAttribute, RegexConfig.ASSET_ATTRIBUTE_RULE, false);

                var triggerAttribute = attributes.FirstOrDefault(x => (string.Equals(x.AttributeType, AttributeTypeConstants.TYPE_DYNAMIC, StringComparison.InvariantCultureIgnoreCase)
                                                                   || string.Equals(x.AttributeType, AttributeTypeConstants.TYPE_RUNTIME, StringComparison.InvariantCultureIgnoreCase))
                                                                   && string.Equals(x.AttributeName, currentAttribute.TriggerAssetAttribute, StringComparison.InvariantCultureIgnoreCase));
                if (triggerAttribute == null)
                {
                    _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, currentAttribute, nameof(AttributeTemplate.TriggerAssetAttribute), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.TRIGGER_ATTRIBUTE }
                    });
                    currentAttribute.TriggerAssetAttributeId = null;
                    return;
                }

                if (triggerAttribute.Id == currentAttribute.AttributeId)
                {
                    _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, currentAttribute, nameof(AttributeTemplate.TriggerAssetAttribute), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.TRIGGER_ATTRIBUTE }
                    });
                    currentAttribute.TriggerAssetAttributeId = null;
                    return;
                }
                currentAttribute.TriggerAssetAttributeId = triggerAttribute.Id;
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, currentAttribute, nameof(AttributeTemplate.TriggerAssetAttribute), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.TRIGGER_ATTRIBUTE }
                });
                currentAttribute.TriggerAssetAttributeId = null;
            }
        }

        private bool ValidateEnableExpression(AttributeTemplate attribute)
        {
            if (string.IsNullOrWhiteSpace(attribute.EnabledExpression))
            {
                _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, attribute, nameof(AttributeTemplate.EnabledExpression), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.ENABLED_EXPRESSION }
                });
                attribute.EnabledExpression = FormatDefaultConstants.ATTRIBUTE_RUNTIME_ENABLE_EXPRESSION_DEFAULT;
                return false;
            }

            var enabled = false;
            var parseData = bool.TryParse(attribute.EnabledExpression, out bool data);

            if (parseData)
                enabled = data;
            else
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AttributeTemplate.EnabledExpression), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.ENABLED_EXPRESSION }
                });
                attribute.EnabledExpression = FormatDefaultConstants.ATTRIBUTE_RUNTIME_ENABLE_EXPRESSION_DEFAULT;
                enabled = false;
            }

            if (!enabled)
            {
                attribute.Expression = null;
                attribute.TriggerAssetAttribute = null;
            }
            return enabled;
        }

        private async Task ValidateDynamicAsync(AttributeTemplate attribute, IEnumerable<AttributeTemplate> currentAttributes, IEnumerable<TemplateMarkupDevice> attributeDeviceMarkups, NpgsqlConnection connection)
        {
            try
            {
                await ValidateDataDeviceTemplateAsync(attribute);

                if (string.IsNullOrEmpty(attribute.DeviceTemplate))
                {
                    attribute.SetDefaultValue(nameof(AttributeTemplate.Device));
                    return;
                }

                ValidateMarkupDevice(attribute, currentAttributes, attributeDeviceMarkups);

                var validateQuery = $@"SELECT
                                        tmp.id AS Template, tmpdtl.key AS Metric, tmpdtl.name as MetricName, tmpdtl.data_type AS DataType
                                    FROM device_templates tmp
                                    LEFT JOIN
                                        (SELECT * FROM template_details
                                         WHERE template_details.key_type_id IN
                                            (SELECT template_key_types.id FROM template_key_types
                                             WHERE template_key_types.name IN (@MetricType, @AggregationType))
                                         AND template_details.name ILIKE @MetricName) tmpdtl
                                        ON tmpdtl.template_payload_id IN (SELECT tmppld.id FROM template_payloads tmppld WHERE tmppld.device_template_id = tmp.id)
                                    WHERE tmp.name ILIKE @TemplateName";

                var result = await connection.QueryFirstOrDefaultAsync<DynamicAttribute>(validateQuery, new
                {
                    TemplateName = attribute.DeviceTemplate.EscapePattern(),
                    MetricName = attribute.Metric.EscapePattern(),
                    MetricType = "metric",
                    AggregationType = "aggregation"
                }, null, commandTimeout: 600);

                if (result == null || result.Template == null)
                {
                    _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AttributeTemplate.DeviceTemplate), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.DEVICE_TEMPLATE }
                    });
                    attribute.SetDefaultValue(nameof(AttributeTemplate.Device));
                    return;
                }

                attribute.DeviceTemplateId = result.Template;
                attribute.DataType = result.DataType;

                if (string.IsNullOrEmpty(result.Metric))
                    throw new EntityNotFoundException();

                attribute.Metric = result.Metric;
                attribute.MetricName = result.MetricName;
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AttributeTemplate.Metric), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.METRIC }
                });
                attribute.Metric = null;
            }
        }

        private async Task ValidateDataDeviceTemplateAsync(AttributeTemplate attribute)
        {
            var columnName = string.Empty;
            var columnValue = string.Empty;
            var propertyName = string.Empty;
            var fields = new Dictionary<(string, string), (string, string)>()
                {
                    { (ErrorProperty.AttributeTemplate.DEVICE_TEMPLATE ,attribute.DeviceTemplate), (nameof(AttributeTemplate.DeviceTemplate), RegexConfig.GENERAL_RULE) },
                    { (ErrorProperty.AttributeTemplate.MARKUP_DEVICE ,attribute.DeviceMarkup), (nameof(AttributeTemplate.DeviceMarkup), RegexConfig.GENERAL_RULE) },
                    { (ErrorProperty.AttributeTemplate.METRIC, attribute.Metric), (nameof(AttributeTemplate.Metric), RegexConfig.METRIC_RULE) }
                };
            foreach (var field in fields)
            {
                try
                {
                    columnName = field.Key.Item1;
                    columnValue = field.Key.Item2;
                    propertyName = field.Value.Item1;
                    var regex = field.Value.Item2;

                    await ValidateColumnDataAsync(columnValue, regex, false);
                }
                catch (MissingFieldException)
                {
                    _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, attribute, propertyName, validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, columnName }
                    });
                    SetDefaultValue(attribute, propertyName);
                }
                catch
                {
                    _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, propertyName, validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, columnName }
                    });
                    SetDefaultValue(attribute, propertyName);
                }
            }
        }

        private async Task ValidateIntegrationAsync(AttributeTemplate attribute, IList<AttributeTemplate> existingIntegrationAttributes)
        {
            try
            {
                await ValidateDataIntegrationAsync(attribute);

                if (string.IsNullOrEmpty(attribute.Channel))
                {
                    attribute.SetDefaultValue(nameof(AttributeTemplate.Channel));
                    return;
                }

                var httpClient = _httpClientFactory.CreateClient(ClientNameConstant.BROKER_SERVICE, _tenantContext);
                var filters = FilterIntegrations.GetFilters(attribute.Channel);
                var query = new FilteredSearchQuery(FilteredSearchQuery.LogicalOp.And, filterObjects: filters);
                var response = await httpClient.SearchAsync<IntegrationAttribute>($"bkr/integrations/search", query);
                var integration = response.Data.First();
                attribute.ChannelId = Guid.Parse(integration.Id);
                await FetchIntegrationDeviceAsync(attribute, httpClient);
                await FetchIntegrationMetricAsync(attribute, httpClient);

                ValidationIntegrationMarkup(attribute, existingIntegrationAttributes);
                var duplicatedAttribute = existingIntegrationAttributes.FirstOrDefault(x => x.AttributeId == attribute.AttributeId);
                if (duplicatedAttribute != null)
                {
                    existingIntegrationAttributes.Remove(duplicatedAttribute);
                }
                existingIntegrationAttributes.Add(attribute);
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AttributeTemplate.Channel), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.CHANNEL }
                });
                attribute.SetDefaultValue(nameof(AttributeTemplate.Channel));
            }
        }

        private void ValidationIntegrationMarkup(AttributeTemplate attribute, IList<AttributeTemplate> existingIntegrationAttributes)
        {
            var sameChannelAttributes = existingIntegrationAttributes.Where(attr => attr.AttributeId != attribute.AttributeId && attr.ChannelId == attribute.ChannelId);
            ValidateChannelMarkupWithDifferentChannel(attribute, existingIntegrationAttributes);
            ValidateChannelMarkupWithSameChannel(attribute, sameChannelAttributes);
            ValidateDeviceMarkupWithSameChannel(attribute, sameChannelAttributes);
        }

        private void ValidateChannelMarkupWithDifferentChannel(AttributeTemplate attribute, IEnumerable<AttributeTemplate> existingIntegrationAttributes)
        {
            // different channel MUST using different Channel Markup
            if (existingIntegrationAttributes.Any(attr => !string.IsNullOrEmpty(attr.ChannelMarkup) && attr.AttributeId != attribute.AttributeId &&
                                              attr.ChannelId != attribute.ChannelId &&
                                              string.Equals(attr.ChannelMarkup, attribute.ChannelMarkup, StringComparison.InvariantCultureIgnoreCase)))
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AttributeTemplate.ChannelMarkup), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.MARKUP_CHANNEL }
                });
                attribute.ChannelMarkup = string.Empty;
            }
        }

        private void ValidateChannelMarkupWithSameChannel(AttributeTemplate attribute, IEnumerable<AttributeTemplate> sameChannelAttributes)
        {
            // same channel MUST using same Channel Markup
            if (sameChannelAttributes.Any(attr => !string.IsNullOrEmpty(attr.ChannelMarkup) && !string.Equals(attr.ChannelMarkup, attribute.ChannelMarkup, StringComparison.InvariantCultureIgnoreCase)))
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AttributeTemplate.ChannelMarkup), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.MARKUP_CHANNEL }
                });
                attribute.ChannelMarkup = string.Empty;
            }
        }

        private void ValidateDeviceMarkupWithSameChannel(AttributeTemplate attribute, IEnumerable<AttributeTemplate> sameChannelAttributes)
        {
            // same channel & different device MUST using different Device Markup
            // same channel & same device MUST using same Device Markup
            if (sameChannelAttributes.Where(x => !string.IsNullOrEmpty(x.DeviceMarkup))
                                     .Any(attr => (attr.DeviceTemplate != attribute.DeviceTemplate &&
                                                  string.Equals(attr.DeviceMarkup, attribute.DeviceMarkup, StringComparison.InvariantCultureIgnoreCase)) ||
                                                  (attr.DeviceTemplate == attribute.DeviceTemplate &&
                                                  !string.Equals(attr.DeviceMarkup, attribute.DeviceMarkup, StringComparison.InvariantCultureIgnoreCase))))
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AttributeTemplate.DeviceMarkup), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.MARKUP_DEVICE }
                });
                attribute.DeviceMarkup = string.Empty;
            }
        }

        private async Task ValidateDataIntegrationAsync(AttributeTemplate attribute)
        {
            var columnName = string.Empty;
            var columnValue = string.Empty;
            var propertyName = string.Empty;
            var fields = new Dictionary<(string, string), (string, string)>()
                {
                    { (ErrorProperty.AttributeTemplate.DEVICE_TEMPLATE ,attribute.DeviceTemplate), (nameof(AttributeTemplate.DeviceTemplate), RegexConfig.GENERAL_RULE) },
                    { (ErrorProperty.AttributeTemplate.MARKUP_DEVICE ,attribute.DeviceMarkup), (nameof(AttributeTemplate.DeviceMarkup), RegexConfig.GENERAL_RULE) },
                    { (ErrorProperty.AttributeTemplate.CHANNEL, attribute.Channel) , (nameof(AttributeTemplate.Channel), RegexConfig.GENERAL_RULE) },
                    { (ErrorProperty.AttributeTemplate.MARKUP_CHANNEL, attribute.ChannelMarkup), ( nameof(AttributeTemplate.ChannelMarkup), RegexConfig.GENERAL_RULE) },
                    { (ErrorProperty.AttributeTemplate.DATA_TYPE, attribute.DataType),(nameof(AttributeTemplate.DataType), RegexConfig.GENERAL_RULE) },
                    { (ErrorProperty.AttributeTemplate.METRIC, attribute.Metric), (nameof(AttributeTemplate.Metric), RegexConfig.METRIC_RULE) }
                };

            foreach (var field in fields)
            {
                try
                {
                    columnName = field.Key.Item1;
                    columnValue = field.Key.Item2;
                    propertyName = field.Value.Item1;
                    var regex = field.Value.Item2;

                    await ValidateColumnDataAsync(columnValue, regex, false);
                }
                catch (MissingFieldException)
                {
                    _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, attribute, propertyName, validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, columnName }
                    });
                    SetDefaultValue(attribute, propertyName);
                }
                catch
                {
                    _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, propertyName, validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, columnName }
                    });
                    SetDefaultValue(attribute, propertyName);
                }
            }
        }

        private void SetDefaultValue(AttributeTemplate attribute, string propertyName)
        {
            switch (propertyName)
            {
                case nameof(AttributeTemplate.DeviceTemplate):
                    attribute.DeviceTemplate = null;
                    break;
                case nameof(AttributeTemplate.DeviceMarkup):
                    attribute.DeviceMarkup = null;
                    break;
                case nameof(AttributeTemplate.Channel):
                    attribute.Channel = null;
                    break;
                case nameof(AttributeTemplate.ChannelMarkup):
                    attribute.ChannelMarkup = null;
                    break;
                case nameof(AttributeTemplate.DataType):
                    attribute.DataType = null;
                    break;
                case nameof(AttributeTemplate.Metric):
                    attribute.Metric = null;
                    break;
            }
        }

        private async Task FetchIntegrationDeviceAsync(AttributeTemplate attribute, HttpClient httpClient)
        {
            try
            {
                var response = await httpClient.GetAsync($"bkr/integrations/{attribute.ChannelId}/fetch?type=devices");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsByteArrayAsync();
                var message = content.Deserialize<BaseSearchResponse<IntegrationDevice>>();
                var devices = message.Data;
                var integrationDevice = devices.FirstOrDefault(x => string.Equals(x.Id, attribute.DeviceTemplate, StringComparison.InvariantCultureIgnoreCase));

                if (integrationDevice == null)
                    throw new EntityNotFoundException();
                attribute.DeviceTemplate = integrationDevice.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Error when call to get external devices.\nTenant: {0} - {1} - {2}\nChannel: {3}",
                                        _tenantContext.TenantId,
                                        _tenantContext.SubscriptionId,
                                        _tenantContext.ProjectId,
                                        attribute.ChannelId));
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AttributeTemplate.DeviceTemplate), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.DEVICE_TEMPLATE }
                });
                attribute.SetDefaultValue(nameof(AttributeTemplate.Device));
            }
        }

        private async Task FetchIntegrationMetricAsync(AttributeTemplate attribute, HttpClient httpClient)
        {
            try
            {
                var response = await httpClient.GetAsync($"bkr/integrations/{attribute.ChannelId}/fetch?type=metrics&data={attribute.DeviceTemplate.ToLower()}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsByteArrayAsync();
                var message = content.Deserialize<BaseSearchResponse<IntegrationMetric>>();
                var devices = message.Data;
                var integrationMetric = devices.FirstOrDefault(x => string.Equals(x.Name, attribute.Metric, StringComparison.InvariantCultureIgnoreCase));

                if (integrationMetric == null)
                    throw new EntityNotFoundException();

                attribute.Metric = integrationMetric.Name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Error when call to get metrics of external device.\nTenant: {0} - {1} - {2}\nChannel: {3} - Device: {4}",
                                        _tenantContext.TenantId,
                                        _tenantContext.SubscriptionId,
                                        _tenantContext.ProjectId,
                                        attribute.ChannelId,
                                        attribute.DeviceTemplate));
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AttributeTemplate.Metric), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.METRIC }
                });
                attribute.Metric = null;
            }
        }

        private async Task ValidateCommandAsync(AttributeTemplate attribute, IEnumerable<AttributeTemplate> currentAttributes, IEnumerable<TemplateMarkupDevice> attributeDeviceMarkups, NpgsqlConnection connection)
        {
            try
            {
                await ValidateDataDeviceTemplateAsync(attribute);

                if (string.IsNullOrEmpty(attribute.DeviceTemplate))
                {
                    attribute.SetDefaultValue(nameof(AttributeTemplate.Device));
                    return;
                }

                ValidateMarkupDevice(attribute, currentAttributes, attributeDeviceMarkups);

                var validateQuery = $@"SELECT
                                        tmp.id AS Template, tmpb.key AS Metric, tmpb.data_type AS DataType
                                    FROM device_templates tmp
                                    LEFT JOIN template_bindings tmpb
                                        ON tmpb.device_template_id = tmp.id AND tmpb.key ILIKE @MetricName
                                    WHERE tmp.name ILIKE @TemplateName";
                var result = await connection.QueryFirstOrDefaultAsync<DynamicAttribute>(validateQuery, new
                {
                    TemplateName = attribute.DeviceTemplate.EscapePattern(),
                    MetricName = attribute.Metric.EscapePattern()
                }, null, commandTimeout: 600);

                if (result == null || result.Template == null)
                {
                    _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AttributeTemplate.DeviceTemplate), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.DEVICE_TEMPLATE }
                    });
                    attribute.SetDefaultValue(nameof(AttributeTemplate.Device));
                    return;
                }

                attribute.DeviceTemplateId = result.Template;
                attribute.DataType = result.DataType;
                attribute.Metric = result.Metric;

                if (string.IsNullOrEmpty(result.Metric))
                    throw new EntityNotFoundException();
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AttributeTemplate.Metric), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.METRIC }
                });
                attribute.Metric = null;
            }
        }

        private void ValidateMarkupDevice(AttributeTemplate attribute, IEnumerable<AttributeTemplate> currentAttributes, IEnumerable<TemplateMarkupDevice> deviceMarkupAttributes)
        {
            var existingAttributes = new List<TemplateMarkupDevice>();

            var attributes = currentAttributes.Where(x => x.IsDynamicAttribute || x.IsCommandAttribute)
                                                        .Select(x => new TemplateMarkupDevice((Guid)x.AttributeId, x.AttributeType, x.DeviceTemplate, x.DeviceMarkup, x.Metric));

            var attrsExisted = deviceMarkupAttributes.Where(x => string.Equals(x.AttributeType, AttributeTypeConstants.TYPE_DYNAMIC, StringComparison.InvariantCultureIgnoreCase)
                                                            || string.Equals(x.AttributeType, AttributeTypeConstants.TYPE_COMMAND, StringComparison.InvariantCultureIgnoreCase));

            existingAttributes.AddRange(attributes);
            foreach (var attr in attrsExisted)
            {
                if (attributes.FirstOrDefault(x => x.AttributeId == attr.AttributeId) == null)
                {
                    existingAttributes.Add(attr);
                }
            }

            foreach (var item in existingAttributes)
            {
                if (attribute.AttributeId == item.AttributeId) // dont validate itself
                    continue;

                if (!string.Equals(item.DeviceTemplate, attribute.DeviceTemplate, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(item.MarkupName, attribute.DeviceMarkup, StringComparison.InvariantCultureIgnoreCase))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AttributeTemplate.DeviceMarkup), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.MARKUP_DEVICE }
                    });
                    attribute.DeviceMarkup = null;
                }
                else if (attribute.IsCommandAttribute
                        && string.Equals(item.DeviceTemplate, attribute.DeviceTemplate, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(item.MarkupName, attribute.DeviceMarkup, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(item.MetricKey, attribute.Metric, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(item.AttributeType, attribute.AttributeType, StringComparison.InvariantCultureIgnoreCase)
                        && item.AttributeId != attribute.AttributeId)
                {
                    _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AttributeTemplate.DeviceMarkup), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.MARKUP_DEVICE }
                    });
                    attribute.DeviceMarkup = null;
                }
            }
        }

        private async Task GetUomAsync(AttributeTemplate attribute, NpgsqlConnection connection)
        {
            try
            {
                if (string.IsNullOrEmpty(attribute.Uom))
                    return;

                await ValidateColumnDataAsync(attribute.Uom, RegexConfig.GENERAL_RULE, true);

                var uomQuery = @"SELECT id as Id, name as Name, abbreviation as Abbreviation FROM uoms WHERE LOWER(abbreviation) = LOWER(@Abbreviation)";
                attribute.UomDetail = await connection.QueryFirstOrDefaultAsync<Uom>(uomQuery, new { Abbreviation = attribute.Uom }, null, commandTimeout: 600);

                if (attribute.UomDetail == null)
                    throw new EntityNotFoundException();

                attribute.UomId = attribute.UomDetail.Id;
                attribute.Uom = attribute.UomDetail.Abbreviation;
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AttributeTemplate.Uom), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AttributeTemplate.UOM }
                });
                attribute.Uom = null;
            }
        }

        private NpgsqlConnection GetDbConnection()
        {
            var connectionString = _configuration["ConnectionStrings:Default"].BuildConnectionString(_configuration, _tenantContext.ProjectId);
            return new NpgsqlConnection(connectionString);
        }

        public async Task ValidateColumnDataAsync(string columnValue, string regexKey, bool allowEmpty = false)
        {
            if (allowEmpty)
                return;

            if (string.IsNullOrEmpty(columnValue))
                throw new MissingFieldException();

            if (columnValue.Length > 255)
                throw new FormatException();

            if (string.IsNullOrEmpty(regexKey))
                return;

            var regexString = await _systemContext.GetValueAsync(regexKey, null);
            var result = Regex.IsMatch(columnValue, regexString, RegexOptions.IgnoreCase);
            if (!result)
            {
                throw new FormatException();
            }
        }
    }

    internal class DynamicAttribute
    {
        public Guid? Template { get; set; }
        public string Metric { get; set; }
        public string MetricName { get; set; }
        public string DataType { get; set; }
    }

    internal class IntegrationAttribute
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    internal class IntegrationDevice
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    internal class IntegrationMetric
    {
        public string Name { get; set; }
    }

    internal class AttributeSimple
    {
        public AttributeSimple(Guid id, string attributeName, string attributeType, string dataType, Guid? templateAttributeId, DateTime updatedUtc)
        {
            Id = id;
            AttributeName = attributeName;
            AttributeType = attributeType;
            DataType = dataType;
            TemplateAttributeId = templateAttributeId;
            UpdatedUtc = updatedUtc;
        }

        public AttributeSimple(Guid id, string attributeName, string attributeType, string dataType, DateTime updatedUtc)
        {
            Id = id;
            AttributeName = attributeName;
            AttributeType = attributeType;
            DataType = dataType;
            UpdatedUtc = updatedUtc;
        }

        public AttributeSimple(Guid id, string attributeName, string attributeType, string dataType)
        {
            Id = id;
            AttributeName = attributeName;
            AttributeType = attributeType;
            DataType = dataType;
        }

        public AttributeSimple(string attributeName, string attributeType)
        {
            AttributeName = attributeName;
            AttributeType = attributeType;
        }

        public Guid Id { get; set; }
        public string AttributeName { get; set; }
        public string AttributeType { get; set; }
        public string DataType { get; set; }
        public Guid? TemplateAttributeId { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }

    internal class TemplateMarkupDevice
    {
        public TemplateMarkupDevice(Guid attributeId, string attributeType, string deviceTemplate, string markupName, string metricKey)
        {
            AttributeId = attributeId;
            AttributeType = attributeType;
            DeviceTemplate = deviceTemplate;
            MarkupName = markupName;
            MetricKey = metricKey;
        }

        public Guid AttributeId { get; set; }
        public string AttributeType { get; set; }
        public string DeviceTemplate { get; set; }
        public string MarkupName { get; set; }
        public string MetricKey { get; set; }
    }
}
