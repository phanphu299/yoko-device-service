using System.Net.Http;
using System.Threading.Tasks;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Device.Function.FileParser.Abstraction;
using Function.Extension;
using System.Collections.Generic;
using System;
using AHI.Infrastructure.SharedKernel.Abstraction;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using AHI.Device.Function.Service.FileImport.Abstraction;
using AHI.Device.Function.FileParser.Constant;
using Microsoft.Azure.WebJobs;
using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.Constant;
using Npgsql;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Device.Function.Model.SearchModel;
using System.Linq;
using Dapper;
using Microsoft.Extensions.Configuration;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Device.Function.FileParser.ErrorTracking.Model;
using System.Globalization;
using AHI.Device.Function.Model.ImportModel.Attribute;
using AHI.Infrastructure.Audit.Constant;
using static AHI.Device.Function.Constant.ErrorMessage;
using System.Text.RegularExpressions;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.SharedKernel.Model;
using AHI.Infrastructure.Interceptor.Abstraction;
using System.Data;
using System.Data.Common;

namespace AHI.Device.Function.Service
{
    public class AssetAttributeParseService : IAssetAttributeParseService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISystemContext _systemContext;
        private readonly ITenantContext _tenantContext;
        private readonly IParserContext _context;
        private readonly ILoggerAdapter<AssetAttributeParseService> _logger;
        private readonly IStorageService _storageService;
        private readonly IAssetAttributeImportService _importService;
        private IImportTrackingService _errorService;
        private readonly IDictionary<string, IImportTrackingService> _errorHandlers;
        private readonly IConfiguration _configuration;
        private readonly IImportNotificationService _notification;
        private readonly IDynamicResolver _dynamicResolver;

        public AssetAttributeParseService(
            IHttpClientFactory httpClientFactory,
            ISystemContext systemContext,
            ITenantContext tenantContext,
            IParserContext context,
            ILoggerAdapter<AssetAttributeParseService> logger,
            IStorageService storageService,
            IAssetAttributeImportService importService,
            IImportTrackingService errorService,
            IDictionary<string, IImportTrackingService> errorHandlers,
            IConfiguration configuration,
            IImportNotificationService notification,
            IDynamicResolver dynamicResolver
        )
        {
            _httpClientFactory = httpClientFactory;
            _systemContext = systemContext;
            _tenantContext = tenantContext;
            _context = context;
            _logger = logger;
            _importService = importService;
            _storageService = storageService;
            _errorService = errorService;
            _errorHandlers = errorHandlers;
            _configuration = configuration;
            _notification = notification;
            _dynamicResolver = dynamicResolver;
        }

        public async Task<AssetAttributeImportResponse> ParseAsync(ParseAssetAttributeMessage message, ExecutionContext context)
        {
            var response = new AssetAttributeImportResponse();
            var mimeType = Constant.EntityFileMapping.GetMimeType(message.ObjectType);
            _errorService = _errorHandlers[mimeType];

            _context.SetContextFormat(ContextFormatKey.DATETIMEFORMAT, message.DateTimeFormat);
            _context.SetContextFormat(ContextFormatKey.DATETIMEOFFSET, DateTimeExtensions.ToValidOffset(message.DateTimeOffset));
            _context.SetExecutionContext(context, ParseAction.IMPORT);

            _notification.Upn = message.Upn;
            _notification.ActivityId = message.ActivityId;
            _notification.ObjectType = message.ObjectType;
            _notification.NotificationType = ActionType.Import;
            // send signalR starting import
            await _notification.SendStartNotifyAsync(1);

            // remove token
            var file = PreProcessFileNames(message.FileName);
            _errorService.File = file;
            using (var stream = new System.IO.MemoryStream())
            {
                await DownloadParseFileAsync(file, stream);
                if (stream.CanRead)
                {
                    try
                    {
                        var fileHandler = _importService.GetFileHandler();
                        var attributes = fileHandler.Handle(stream);
                        var validAttributes = await GetAttributeDetailAsync(attributes, message.UnsavedAttributes, message.AssetId);
                        response.Attributes = validAttributes;
                    }
                    catch (Exception ex)
                    {
                        _errorService.RegisterError(ex.Message, Constant.ErrorType.UNDEFINED);
                        _logger.LogError(ex, ex.Message);
                    }
                }
            }

            var errors = _errorService.FileErrors[message.FileName];

            if (errors != null && errors.Any())
            {
                var trackErrors = new List<ErrorDetail>();
                foreach (ExcelTrackError error in errors.Cast<ExcelTrackError>())
                {
                    if (trackErrors.Any(x => x.Column == error.Column && x.Row == error.Row))
                        continue;

                    var errorMessage = new ErrorDetail()
                    {
                        Column = error.Column,
                        Detail = error.Message,
                        Row = error.Row,
                        Type = error.Type,
                        ColumnName = error.ValidationInfo != null ? error.ValidationInfo["PropertyName"]?.ToString() : string.Empty
                    };
                    trackErrors.Add(errorMessage);
                }
                response.Errors = trackErrors;
            }
            else
            {
                await _notification.SendFinishImportNotifyAsync(ActionStatus.Success, (response.Attributes.Count(), 0));
            }

            return response;
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

        private string PreProcessFileNames(string fileName)
        {
            return StringExtension.RemoveFileToken(fileName);
        }

        private async Task<IEnumerable<AssetAttribute>> GetAttributeDetailAsync(
            IEnumerable<AssetAttribute> attributes,
            IEnumerable<UnsaveAttribute> unSaveAttributes,
            string assetId)
        {
            IEnumerable<AssetAttribute> validAttributes;
            attributes.ToList().ForEach(x => x.Id = Guid.NewGuid());
            using (var connection = GetDbConnection())
            {
                await connection.OpenAsync();

                var existingAttributes = unSaveAttributes.Select(x => new AttributeSimple(x.Id == Guid.Empty ? Guid.NewGuid() : x.Id, x.Name, x.AttributeType, x.DataType, x.TemplateAttributeId, x.UpdatedUtc)).ToList();
                validAttributes = HandleDuplicateAttribute(attributes, existingAttributes);
                var existingCommandAttributes = new List<AssetAttribute>(unSaveAttributes
                                                                    .Where(x => string.Equals(x.AttributeType, AttributeTypeConstants.TYPE_COMMAND, StringComparison.InvariantCultureIgnoreCase))
                                                                    .Select(x => new AssetAttribute { Id = x.Id, AttributeType = x.AttributeType, DeviceId = x.DeviceId, Metric = x.MetricKey }));

                foreach (var attr in validAttributes)
                {
                    SetDefaultByAttributeType(attr);
                    await ValidateAttributeTypeDependenciesAsync(attr, assetId, existingCommandAttributes, connection);
                    ValidateDataType(attr);
                    await ValidateDataTypeDependenciesAsync(attr, validAttributes, existingAttributes);
                    if (!attr.IsCommandAttribute && !attr.IsAliasAttribute)
                        await GetUomAsync(attr, connection);

                    connection.Close();
                }
            }

            return validAttributes;
        }

        private IEnumerable<AssetAttribute> HandleDuplicateAttribute(IEnumerable<AssetAttribute> attributes, IEnumerable<AttributeSimple> existingAttributes)
        {
            var nonDuplicateAttributes = new List<AssetAttribute>();
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
                    attr.Id = existingAttribute.Id;
                    attr.UpdatedUtc = existingAttribute.UpdatedUtc;
                }
                nonDuplicateAttributes.Add(attr);
            }
            return nonDuplicateAttributes;
        }

        public async Task ValidateAttributeTypeDependenciesAsync(AssetAttribute attribute, string assetId, ICollection<AssetAttribute> existingCommandAttributes, IDbConnection connection)
        {
            switch (attribute.AttributeType)
            {
                case AttributeTypeConstants.TYPE_DYNAMIC:
                    await ValidateDynamicAttributeAsync(attribute, connection);
                    break;
                case AttributeTypeConstants.TYPE_RUNTIME:
                    ValidateEnableExpression(attribute);
                    break;
                case AttributeTypeConstants.TYPE_ALIAS:
                    await ValidateAliasAttributeAsync(attribute, assetId, connection);
                    break;
                case AttributeTypeConstants.TYPE_INTEGRATION:
                    await ValidateIntegrationAttributeAsync(attribute);
                    break;
                case AttributeTypeConstants.TYPE_COMMAND:
                    await ValidateCommandAttributeAsync(attribute, existingCommandAttributes, connection);
                    break;
            }
        }

        private async Task ValidateDataTypeDependenciesAsync(AssetAttribute attribute, IEnumerable<AssetAttribute> attributes, IEnumerable<AttributeSimple> existingAttributes)
        {
            if (attribute.IsRuntimeAttribute)
            {
                var attrs = new List<AttributeSimple>();
                attrs.AddRange(attributes.Select(x => new AttributeSimple((Guid)x.Id, x.AttributeName, x.AttributeType, x.DataType)));
                var attributeIds = attributes.Select(x => x.Id);
                attrs.AddRange(existingAttributes.Where(x => !attributeIds.Contains(x.Id)));

                if (!string.Equals(attribute.EnabledExpression, FormatDefaultConstants.ATTRIBUTE_RUNTIME_ENABLE_EXPRESSION_DEFAULT, StringComparison.InvariantCultureIgnoreCase))
                {
                    ValidateExpression(attribute, attrs);
                    ValidateTriggerAttribute(attribute, attrs);
                }
            }
            else if (attribute.IsStaticAttribute)
            {
                await ValidateValueAttributeAsync(attribute);
            }

            if (!attribute.IsAliasAttribute && !attribute.IsCommandAttribute)
            {
                if (attribute.DataType == DataTypeConstants.TYPE_DOUBLE || attribute.DataType == DataTypeConstants.TYPE_INTEGER)
                {
                    ValidateDecimalPlace(attribute);
                    ValidateThousandSeparator(attribute);
                }
            }
        }

        private async Task<IEnumerable<AttributeSimple>> GetAssetAttributesAsync(string assetId, IDbConnection connection)
        {
            var result = new List<AttributeSimple>();

            var queryAssetAttribute = $@"SELECT id as Id,
                                            name as AttributeName,
                                            attribute_type as AttributeType,
                                            data_type as DataType
                                        FROM asset_attributes
                                        WHERE asset_id = @AssetId";
            var assetAttributes = await connection.QueryAsync<AttributeSimple>(queryAssetAttribute, new { AssetId = Guid.Parse(assetId) }, null, commandTimeout: 600);
            result.AddRange(assetAttributes);

            var queryTemplateAttribute = $@"SELECT
                                                CASE
                                                    WHEN att.attribute_type = 'static' THEN attstamp.id
                                                    WHEN att.attribute_type = 'alias' THEN attalsm.id
                                                    WHEN att.attribute_type = 'dynamic' THEN attdymp.id
                                                    WHEN att.attribute_type = 'command' THEN attcmdm.id
                                                    WHEN att.attribute_type = 'runtime' THEN attrtm.id
                                                    WHEN att.attribute_type = 'integration' THEN attintm.id
                                                END AS Id,
                                                att.name as AttributeName,
                                                att.attribute_type as AttributeType,
                                                att.data_type as DataType
                                            FROM assets a
                                            LEFT JOIN asset_templates atpl ON atpl.id = a.asset_template_id
                                            LEFT JOIN asset_attribute_templates att on atpl.id = att.asset_template_id
                                            LEFT JOIN asset_attribute_static_mapping attstamp ON a.id = attstamp.asset_id and att.id = attstamp.asset_attribute_template_id
                                            LEFT JOIN asset_attribute_alias_mapping attalsm ON a.id = attalsm.asset_id and att.id = attalsm.asset_attribute_template_id
                                            LEFT JOIN asset_attribute_dynamic_mapping attdymp ON a.id = attdymp.asset_id and att.id = attdymp.asset_attribute_template_id
                                            LEFT JOIN asset_attribute_command_mapping attcmdm ON a.id = attcmdm.asset_id and att.id = attcmdm.asset_attribute_template_id
                                            LEFT JOIN asset_attribute_runtime_mapping attrtm ON a.id = attrtm.asset_id and att.id = attrtm.asset_attribute_template_id
                                            LEFT JOIN asset_attribute_integration_mapping attintm ON a.id = attintm.asset_id and att.id = attintm.asset_attribute_template_id
                                            WHERE a.id = @AssetId";
            var assetTemplateAttributes = await connection.QueryAsync<AttributeSimple>(queryTemplateAttribute, new { AssetId = Guid.Parse(assetId) }, null, commandTimeout: 600);
            result.AddRange(assetTemplateAttributes);

            return result;
        }

        private async Task ValidateValueAttributeAsync(AssetAttribute attribute)
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
                    var offset = TimeSpan.Parse(timezoneOffset);
                    if (DateTime.TryParseExact(attribute.Value, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var datetime))
                    {
                        var datetimeOffsetValue = new DateTimeOffset(datetime, TimeSpan.Zero).ToOffset(offset);
                        attribute.Value = datetimeOffsetValue.ToString(AHI.Infrastructure.SharedKernel.Extension.Constant.DefaultDateTimeFormat);
                    }
                    else
                        throw new EntityInvalidException();
                }
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AssetAttribute.Value), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.VALUE }
                });
                attribute.Value = null;
            }
        }

        private async Task ValidateDynamicAttributeAsync(AssetAttribute attribute, IDbConnection connection)
        {
            await ValidateDeviceAsync(attribute, connection);
            await ValidateDeviceMetricAsync(attribute, connection);
        }

        private async Task ValidateDeviceAsync(AssetAttribute attribute, IDbConnection connection)
        {
            try
            {
                if (string.IsNullOrEmpty(attribute.DeviceId))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, attribute, nameof(AssetAttribute.DeviceId), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.DEVICE_ID }
                    });

                    attribute.SetDefaultValue(nameof(AssetAttribute.DeviceId));
                    return;
                }

                var deviceQuery = @$"SELECT id FROM devices WHERE id ILIKE @DeviceId";

                var deviceId = await connection.QueryFirstOrDefaultAsync<string>(deviceQuery, new { DeviceId = attribute.DeviceId.EscapePattern() }, null, commandTimeout: 600);

                if (string.IsNullOrEmpty(deviceId))
                    throw new EntityNotFoundException();

                attribute.DeviceId = deviceId;
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AssetAttribute.DeviceId), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.DEVICE_ID }
                });
                attribute.SetDefaultValue(nameof(AssetAttribute.DeviceId));
                return;
            }
        }

        private async Task ValidateDeviceMetricAsync(AssetAttribute attribute, IDbConnection connection)
        {
            try
            {
                if (string.IsNullOrEmpty(attribute.Metric))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, attribute, nameof(AssetAttribute.Metric), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.METRIC }
                    });

                    attribute.SetDefaultValue(nameof(AssetAttribute.Metric));
                    return;
                }

                var metricQuery = @$"SELECT
                                        detail.key AS MetricKey, detail.data_type AS DataType
                                    FROM devices dv
                                    LEFT JOIN device_templates tmp ON dv.device_template_id = tmp.id
                                    LEFT JOIN template_payloads payload ON tmp.id = payload.device_template_id
                                    LEFT JOIN template_details detail ON payload.id = detail.template_payload_id
                                    LEFT JOIN template_key_types tkt ON detail.key_type_id = tkt.id
                                    WHERE dv.id ILIKE @DeviceId AND detail.name ILIKE @MetricName AND tkt.name IN (@MetricType, @AggregationType)";

                var (metricKey, dataType) = await connection.QueryFirstOrDefaultAsync<(string, string)>(metricQuery,
                 new
                 {
                     DeviceId = attribute.DeviceId.EscapePattern(),
                     MetricName = attribute.Metric.EscapePattern(),
                     MetricType = TemplateKeyTypes.METRIC,
                     AggregationType = TemplateKeyTypes.AGGREGATION
                 }, null, commandTimeout: 600);

                if (metricKey == null || dataType == null)
                    throw new EntityNotFoundException();

                attribute.Metric = metricKey;
                attribute.DataType = dataType;
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AssetAttribute.Metric), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.METRIC }
                });
                attribute.SetDefaultValue(nameof(AssetAttribute.Metric));
                return;
            }
        }

        private async Task ValidateAliasAttributeAsync(AssetAttribute attribute, string assetId, IDbConnection connection)
        {
            await ValidateAliasAssetAsync(attribute, assetId, connection);
            await ValidateAliasAssetAttributeAsync(attribute, connection);
        }

        private async Task ValidateAliasAssetAsync(AssetAttribute attribute, string entityId, IDbConnection connection)
        {
            try
            {
                if (string.IsNullOrEmpty(attribute.AliasAsset))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, attribute, nameof(AssetAttribute.AliasAsset), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.ALIAS_ASSET }
                    });
                    attribute.SetDefaultValue(nameof(AssetAttribute.AliasAsset));
                    return;
                }

                if (!Guid.TryParse(attribute.AliasAsset, out var assetId))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AssetAttribute.AliasAsset), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.ALIAS_ASSET }
                    });
                    attribute.SetDefaultValue(nameof(AssetAttribute.AliasAsset));
                }

                if (assetId == Guid.Parse(entityId))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AssetAttribute.AliasAsset), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.ALIAS_ASSET }
                    });
                    attribute.SetDefaultValue(nameof(AssetAttribute.AliasAsset));
                    return;
                }

                var aliasAssetQuery = @$"SELECT name FROM assets WHERE id = @AliasAsset;";
                var assetName = await connection.ExecuteScalarAsync<string>(aliasAssetQuery, new { AliasAsset = assetId }, commandTimeout: 600);
                if (string.IsNullOrEmpty(assetName))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AssetAttribute.AliasAsset), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.ALIAS_ASSET }
                    });
                    attribute.SetDefaultValue(nameof(AssetAttribute.AliasAsset));
                }
                attribute.AliasAssetName = assetName;
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AssetAttribute.AliasAsset), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.ALIAS_ASSET }
                });
                attribute.SetDefaultValue(nameof(AssetAttribute.AliasAsset));
            }
        }

        private async Task ValidateAliasAssetAttributeAsync(AssetAttribute attribute, IDbConnection connection)
        {
            try
            {
                if (string.IsNullOrEmpty(attribute.AliasAttribute))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, attribute, nameof(AssetAttribute.AliasAttribute), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.ALIAS_ATTRIBUTE }
                    });

                    attribute.SetDefaultValue(nameof(AssetAttribute.AliasAttribute));
                    return;
                }

                var aliasAssetAttributes = await GetAssetAttributesAsync(attribute.AliasAsset, connection);
                var aliasAttribute = aliasAssetAttributes.FirstOrDefault(x => x.AttributeName.Equals(attribute.AliasAttribute, StringComparison.InvariantCultureIgnoreCase));
                if (aliasAttribute == null)
                    throw new EntityNotFoundException();

                attribute.AliasAttribute = aliasAttribute.AttributeName;
                attribute.DataType = aliasAttribute.DataType;

                if (aliasAttribute.AttributeType == AttributeTypeConstants.TYPE_COMMAND)
                {
                    RegisterAliasAttributeError(attribute);
                    return;
                }

                attribute.AliasAttributeId = aliasAttribute.Id;
                var containRecursiveAssetAttribute = await CheckRecursiveAssetAttribute(attribute.Id, aliasAttribute.Id, connection);
                if (containRecursiveAssetAttribute)
                {
                    RegisterAliasAttributeError(attribute);
                    attribute.AliasAttributeId = Guid.Empty;
                    return;
                }
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AssetAttribute.AliasAttribute), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.ALIAS_ATTRIBUTE }
                });
                attribute.SetDefaultValue(nameof(AssetAttribute.AliasAttribute));
            }
        }

        private void RegisterAliasAttributeError(AssetAttribute attribute)
        {
            _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AssetAttribute.AliasAttribute), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.ALIAS_ATTRIBUTE }
                });
            attribute.SetDefaultValue(nameof(AssetAttribute.AliasAttribute));
        }

        private bool ValidateEnableExpression(AssetAttribute attribute)
        {
            if (string.IsNullOrEmpty(attribute.EnabledExpression))
            {
                _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, attribute, nameof(AssetAttribute.EnabledExpression), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.ENABLED_EXPRESSION }
                    });

                attribute.SetDefaultValue(nameof(AssetAttribute.EnabledExpression));
                return false;
            }

            var result = bool.TryParse(attribute.EnabledExpression, out bool enabledExpression);

            if (!result)
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AssetAttribute.EnabledExpression), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.ENABLED_EXPRESSION }
                });

                attribute.SetDefaultValue(nameof(AssetAttribute.EnabledExpression));
                return false;
            }

            if (!enabledExpression)
            {
                attribute.Expression = null;
                attribute.TriggerAttribute = null;
            }

            return enabledExpression;
        }

        private void ValidateExpression(AssetAttribute currentAttribute, IEnumerable<AttributeSimple> attributes)
        {
            if (string.IsNullOrWhiteSpace(currentAttribute.Expression))
            {
                _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, currentAttribute, nameof(AssetAttribute.Expression), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.EXPRESSION }
                });
                return;
            }

            var expression = currentAttribute.Expression.PreProcessExpression();
            var expressionValidate = expression;
            bool hasCommandAttribute = false;
            var dictionary = new Dictionary<string, object>();

            foreach (var attr in attributes)
            {
                ProcessAttributeExpression(attr, dictionary, ref hasCommandAttribute, ref expression, ref expressionValidate);
            }

            if (attributes.Any(x => expression.Contains(x.Id.ToString()) && !expression.Contains($"${{{x.Id}}}$")) ||
                expression.Contains(currentAttribute.Id.ToString()))
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, currentAttribute, nameof(AssetAttribute.Expression), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.EXPRESSION }
                });
                currentAttribute.Expression = null;
                return;
            }

            if (currentAttribute.DataType == DataTypeConstants.TYPE_TEXT
                && !attributes.Any(x => expressionValidate.Contains($"request[\"{x.Id}\"]")))
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

                if (hasCommandAttribute)
                {
                    _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, currentAttribute, nameof(AssetAttribute.Expression), validationInfo: new Dictionary<string, object>
                        {
                            { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.EXPRESSION }
                        });
                    currentAttribute.Expression = null;
                    return;
                }

                var value = _dynamicResolver.ResolveInstance("return true;", expressionValidate).OnApply(dictionary);
                if (!string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    StringExtension.ParseValue(value.ToString(), currentAttribute.DataType, null);
                    Regex regex = new Regex(RegexConstants.PATTERN_REPLACE_EXPRESSION);
                    currentAttribute.Expression = regex.Replace(expression, "");
                }
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, currentAttribute, nameof(AssetAttribute.Expression), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.EXPRESSION }
                });
                currentAttribute.Expression = null;
            }
        }

        private void ProcessAttributeExpression(AttributeSimple attr, Dictionary<string, object> dictionary,
                                                ref bool hasCommandAttribute, ref string expression, ref string expressionValidate)
        {
            if (string.IsNullOrEmpty(attr.DataType))
                return;

            object draftValue = null;
            var currentExpression = expression;

            string attributeName = attr.AttributeName;
            if (attr.AttributeName.Contains(RegexConstants.EXPRESSION_REFER_CLOSE.Trim()))
            {
                attributeName = $"{attr.AttributeName.Replace(RegexConstants.EXPRESSION_REFER_CLOSE.Trim(), string.Empty).Trim()}{RegexConstants.EXPRESSION_REFER_CLOSE}";
            }
            expression = expression.Replace($"{RegexConstants.EXPRESSION_REFER_OPEN}{attributeName}{RegexConstants.EXPRESSION_REFER_CLOSE}", $"${{{attr.Id}}}$", ignoreCase: true, null);
            if (attr.AttributeType == AttributeTypeConstants.TYPE_COMMAND && expression != currentExpression)
                hasCommandAttribute = true;

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

        private void ValidateTriggerAttribute(AssetAttribute currentAttribute, IEnumerable<AttributeSimple> attributes)
        {
            if (string.IsNullOrEmpty(currentAttribute.TriggerAttribute))
                return;

            var triggerAttribute = attributes.FirstOrDefault(x => string.Equals(x.AttributeName, currentAttribute.TriggerAttribute, StringComparison.InvariantCultureIgnoreCase));
            if (triggerAttribute == null)
            {
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, currentAttribute, nameof(AssetAttribute.TriggerAttribute), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.TRIGGER_ATTRIBUTE }
                });
                currentAttribute.TriggerAttributeId = null;
                return;
            }
            if ((!string.Equals(triggerAttribute.AttributeType, AttributeTypeConstants.TYPE_DYNAMIC, StringComparison.InvariantCultureIgnoreCase)
                && !string.Equals(triggerAttribute.AttributeType, AttributeTypeConstants.TYPE_RUNTIME, StringComparison.InvariantCultureIgnoreCase))
                || string.Equals(currentAttribute.TriggerAttribute, currentAttribute.AttributeName, StringComparison.InvariantCultureIgnoreCase))
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, currentAttribute, nameof(AssetAttribute.TriggerAttribute), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.TRIGGER_ATTRIBUTE }
                });
                currentAttribute.TriggerAttributeId = null;
                return;
            }
            currentAttribute.TriggerAttribute = triggerAttribute.AttributeName;
            currentAttribute.TriggerAttributeId = triggerAttribute.Id;
        }

        private async Task ValidateIntegrationAttributeAsync(AssetAttribute attribute)
        {
            try
            {
                if (string.IsNullOrEmpty(attribute.Channel))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, attribute, nameof(AssetAttribute.Channel), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.CHANNEL }
                    });

                    attribute.SetDefaultValue(nameof(AssetAttribute.Channel));
                    return;
                }

                var httpClient = _httpClientFactory.CreateClient(ClientNameConstant.BROKER_SERVICE, _tenantContext);
                var filters = FilterIntegrations.GetFilters(attribute.Channel);
                var query = new FilteredSearchQuery(FilteredSearchQuery.LogicalOp.And, filterObjects: filters);
                var response = await httpClient.SearchAsync<IntegrationDto>($"bkr/integrations/search", query);
                var integration = response.Data.FirstOrDefault();
                if (integration == null)
                {
                    _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AssetAttribute.Channel), validationInfo: new Dictionary<string, object>
                        {
                            { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.CHANNEL }
                        });

                    attribute.SetDefaultValue(nameof(AssetAttribute.Channel));
                    return;
                }
                attribute.ChannelId = Guid.Parse(integration.Id);

                await FetchIntegrationDeviceAsync(attribute, httpClient);
                await FetchIntegrationMetricAsync(attribute, httpClient);
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AssetAttribute.Channel), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.CHANNEL }
                });
                attribute.SetDefaultValue(nameof(AssetAttribute.Channel));
            }
        }

        private async Task FetchIntegrationDeviceAsync(AssetAttribute attribute, HttpClient httpClient)
        {
            try
            {
                if (string.IsNullOrEmpty(attribute.DeviceId))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, attribute, nameof(AssetAttribute.DeviceId), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.DEVICE_ID }
                    });

                    attribute.SetDefaultValue(nameof(AssetAttribute.DeviceId));
                    return;
                }

                var response = await httpClient.GetAsync($"bkr/integrations/{attribute.ChannelId}/fetch?type=devices");
                response.EnsureSuccessStatusCode();
                var message = await response.Content.ReadAsByteArrayAsync();
                var integrationDevice = message.Deserialize<BaseSearchResponse<IntegrationDeviceDto>>();
                var device = integrationDevice.Data.FirstOrDefault(x => string.Equals(x.Id, attribute.DeviceId, StringComparison.InvariantCultureIgnoreCase));
                if (device == null)
                    throw new EntityNotFoundException();
                attribute.DeviceId = device.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when call to get external devices");
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AssetAttribute.DeviceId), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.DEVICE_ID }
                    });
                attribute.SetDefaultValue(nameof(AssetAttribute.DeviceId));
                return;
            }
        }

        private async Task FetchIntegrationMetricAsync(AssetAttribute attribute, HttpClient httpClient)
        {
            try
            {
                if (string.IsNullOrEmpty(attribute.Metric))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, attribute, nameof(AssetAttribute.Metric), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.METRIC }
                    });

                    attribute.SetDefaultValue(nameof(AssetAttribute.Metric));
                    return;
                }

                var response = await httpClient.GetAsync($"bkr/integrations/{attribute.ChannelId}/fetch?type=metrics&data={attribute.DeviceId.ToLower()}");
                response.EnsureSuccessStatusCode();
                var message = await response.Content.ReadAsByteArrayAsync();
                var integrationMetric = message.Deserialize<BaseSearchResponse<IntegrationMetricDto>>();
                var metric = integrationMetric.Data.FirstOrDefault(x => string.Equals(x.Name, attribute.Metric, StringComparison.InvariantCultureIgnoreCase));
                if (metric == null)
                    throw new EntityNotFoundException();
                attribute.Metric = metric.Name;
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AssetAttribute.Metric), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.METRIC }
                    });
                attribute.SetDefaultValue(nameof(AssetAttribute.Metric));
                return;
            }
        }

        private async Task ValidateCommandAttributeAsync(AssetAttribute attribute, ICollection<AssetAttribute> attributeCommands, IDbConnection connection)
        {
            await ValidateDeviceAsync(attribute, connection);
            await ValidateMetricBindingAsync(attribute, connection);
            if (attribute.DeviceId != null && attribute.Metric != null)
                CheckDuplicateCommandAttribute(attribute, attributeCommands);
        }

        private async Task ValidateMetricBindingAsync(AssetAttribute attribute, IDbConnection connection)
        {
            try
            {
                if (string.IsNullOrEmpty(attribute.Metric))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_DEPENDENCES_REQUIRED, attribute, nameof(AssetAttribute.Metric), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.METRIC }
                    });

                    attribute.SetDefaultValue(nameof(AssetAttribute.Metric));
                    return;
                }

                var metricQuery = @$"SELECT
                                        binding.key AS MetricKey, binding.data_type AS DataType
                                    FROM devices dv
                                    LEFT JOIN device_templates tmp ON dv.device_template_id = tmp.id
                                    LEFT JOIN template_bindings binding ON tmp.id = binding.device_template_id
                                    WHERE dv.id ILIKE @DeviceId AND binding.key ILIKE @MetricKey";

                var (metricKey, dataType) = await connection.QueryFirstOrDefaultAsync<(string, string)>(metricQuery, new { DeviceId = attribute.DeviceId.EscapePattern(), MetricKey = attribute.Metric.EscapePattern() }, null, commandTimeout: 600);

                if (metricKey == null || dataType == null)
                    throw new EntityNotFoundException();

                attribute.Metric = metricKey;
                attribute.DataType = dataType;

                var usingMetricQuery = @$"SELECT Count(attr_cmd.metric_key)
                                                FROM asset_attributes attr
                                                LEFT JOIN asset_attribute_commands attr_cmd ON attr.id = attr_cmd.asset_attribute_id
                                                WHERE attr_cmd.device_id ILIKE @DeviceId
                                                             and attr_cmd.metric_key ILIKE @MetricKey
                                                             and attr.id != @AttributeId;

                                            SELECT Count(attr_cmd_mp.metric_key)
                                                FROM assets
                                                LEFT JOIN asset_attribute_command_mapping attr_cmd_mp ON assets.id = attr_cmd_mp.asset_id
                                                WHERE attr_cmd_mp.device_id ILIKE @DeviceId and attr_cmd_mp.metric_key ILIKE @MetricKey;";

                var countUsingMetric = 0;
                var param = new
                {
                    DeviceId = attribute.DeviceId.EscapePattern(),
                    MetricKey = attribute.Metric.EscapePattern(),
                    AttributeId = attribute.Id
                };
                using (var multi = await connection.QueryMultipleAsync(usingMetricQuery, param))
                {
                    countUsingMetric = multi.ReadSingle<int>();
                    countUsingMetric += multi.ReadSingle<int>();
                }

                if (countUsingMetric > 0)
                {
                    _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AssetAttribute.Metric), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.METRIC }
                    });
                    attribute.SetDefaultValue(nameof(AssetAttribute.Metric));
                }
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AssetAttribute.Metric), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.METRIC }
                });
                attribute.SetDefaultValue(nameof(AssetAttribute.Metric));
                return;
            }
        }

        private void CheckDuplicateCommandAttribute(AssetAttribute attribute, ICollection<AssetAttribute> existingCommandAttributes)
        {
            if (existingCommandAttributes.Any(x => string.Equals(x.DeviceId, attribute.DeviceId, StringComparison.InvariantCultureIgnoreCase)
                                                && string.Equals(x.Metric, attribute.Metric, StringComparison.InvariantCultureIgnoreCase)
                                                && x.Id != attribute.Id))
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AssetAttribute.Metric), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.METRIC },
                });
                attribute.SetDefaultValue(nameof(AssetAttribute.Metric));
            }
            existingCommandAttributes.Add(attribute);
        }

        private async Task<bool> CheckRecursiveAssetAttribute(Guid attributeId, Guid aliasAttributeId, IDbConnection connection)
        {
            var hasRecursiveAttribute = false;
            try
            {
                var snapshotValues = await connection.QueryAsync<Guid>($@"select attribute_id from find_root_alias_asset_attribute(@AliasAttributeId)", new { AliasAttributeId = aliasAttributeId });
                hasRecursiveAttribute = snapshotValues.Contains(attributeId);
            }
            catch (NpgsqlException exc)
            {
                _logger.LogError(exc, exc.Message);
                _errorService.RegisterError(exc.Message, validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.ALIAS_ATTRIBUTE }
                });
            }
            return hasRecursiveAttribute;
        }

        private async Task GetUomAsync(AssetAttribute attribute, IDbConnection connection)
        {
            try
            {
                if (attribute.Uom is null)
                    return;

                await ValidateColumnDataAsync(attribute.Uom, RegexConfig.GENERAL_RULE, true);

                var uomQuery = @"SELECT id, name, abbreviation FROM uoms WHERE abbreviation ILIKE @Abbreviation";
                var uom = await connection.QueryFirstOrDefaultAsync<UomDto>(uomQuery, new { Abbreviation = attribute.Uom.EscapePattern() }, null, commandTimeout: 600);
                attribute.UomId = uom.Id;
                attribute.UomData = uom;
            }
            catch
            {
                _errorService.RegisterError(ParseValidation.PARSER_REFERENCES_DATA_NOT_EXISTS, attribute, nameof(AssetAttribute.Uom), validationInfo: new Dictionary<string, object>
                {
                    { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.UOM }
                });
                attribute.SetDefaultValue(nameof(AssetAttribute.Uom));
            }
        }

        public async Task ValidateColumnDataAsync(string columnValue, string regexKey, bool allowEmpty = false)
        {
            if (allowEmpty)
                return;

            if (string.IsNullOrEmpty(columnValue) || columnValue.Length > 255)
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

        private void ValidateDataType(AssetAttribute attribute)
        {
            string validDataType = null;

            if (attribute.IsRuntimeAttribute || attribute.IsIntegrationAttribute)
                validDataType = DataTypeExtensions.ATTRIBUTE_DATA_TYPES.Where(x => x != DataTypeConstants.TYPE_DATETIME)
                                .FirstOrDefault(x => string.Equals(x, attribute.DataType, StringComparison.InvariantCultureIgnoreCase));
            else
                // for type dynamic, alias, command, data type should already be correct anyway
                validDataType = DataTypeExtensions.ATTRIBUTE_DATA_TYPES
                                .FirstOrDefault(x => string.Equals(x, attribute.DataType, StringComparison.InvariantCultureIgnoreCase));

            if (validDataType != null)
            {
                attribute.DataType = validDataType;
                return;
            }
            else
            {
                _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AssetAttribute.DataType), validationInfo: new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.DATA_TYPE },
                        { ErrorProperty.ERROR_PROPERTY_VALUE, attribute.DataType }
                    });
                attribute.DataType = DataTypeConstants.TYPE_TEXT;
            }
        }

        private void ValidateDecimalPlace(AssetAttribute attribute)
        {
            if (attribute.DataType != DataTypeConstants.TYPE_DOUBLE)
                return;

            if (!string.IsNullOrWhiteSpace(attribute.DecimalPlace))
            {
                var result = int.TryParse(attribute.DecimalPlace, out int decimalPlace);

                if (result && decimalPlace >= FormatDefaultConstants.ATTRIBUTE_DECIMAL_PLACES_MIN && decimalPlace <= FormatDefaultConstants.ATTRIBUTE_DECIMAL_PLACES_MAX)
                    return;
                else
                {
                    _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AssetAttribute.DecimalPlace), validationInfo: new Dictionary<string, object>
                        {
                            { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.DECIMAL_PLACES }
                        });
                    attribute.DecimalPlace = string.Empty;
                };
            }
        }

        private void ValidateThousandSeparator(AssetAttribute attribute)
        {
            if (attribute.DataType != DataTypeConstants.TYPE_DOUBLE)
            {
                attribute.ThousandSeparator = FormatDefaultConstants.ATTRIBUTE_THOUSAND_SEPARATOR_DEFAULT;
                return;
            }

            if (!string.IsNullOrWhiteSpace(attribute.ThousandSeparator))
            {
                var result = bool.TryParse(attribute.ThousandSeparator, out bool thousandSeparator);

                if (result)
                    return;
                else
                {
                    _errorService.RegisterError(ParseValidation.PARSER_INVALID_DATA, attribute, nameof(AssetAttribute.ThousandSeparator), validationInfo: new Dictionary<string, object>
                        {
                            { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetAttribute.THOUSAND_SEPARATOR }
                        });
                    attribute.ThousandSeparator = FormatDefaultConstants.ATTRIBUTE_THOUSAND_SEPARATOR_DEFAULT;
                };
            }
            else
            {
                if (attribute.IsDynamicAttribute)
                    attribute.ThousandSeparator = FormatDefaultConstants.ATTRIBUTE_THOUSAND_SEPARATOR_DEFAULT;
            }
        }

        private void SetDefaultByAttributeType(AssetAttribute attribute)
        {
            switch (attribute.AttributeType)
            {
                case AttributeTypeConstants.TYPE_STATIC:
                    attribute.EnabledExpression = null;
                    attribute.Expression = null;
                    attribute.TriggerAttribute = null;
                    attribute.DeviceId = null;
                    attribute.Metric = null;
                    attribute.Channel = null;
                    attribute.AliasAsset = null;
                    attribute.AliasAttribute = null;
                    break;
                case AttributeTypeConstants.TYPE_ALIAS:
                    attribute.EnabledExpression = null;
                    attribute.Expression = null;
                    attribute.TriggerAttribute = null;
                    attribute.DeviceId = null;
                    attribute.Metric = null;
                    attribute.Channel = null;
                    attribute.Metric = null;
                    attribute.Value = null;
                    break;
                case AttributeTypeConstants.TYPE_COMMAND:
                    attribute.EnabledExpression = null;
                    attribute.Expression = null;
                    attribute.TriggerAttribute = null;
                    attribute.Channel = null;
                    attribute.AliasAsset = null;
                    attribute.AliasAttribute = null;
                    attribute.Value = null;
                    break;
                case AttributeTypeConstants.TYPE_DYNAMIC:
                    attribute.EnabledExpression = null;
                    attribute.Expression = null;
                    attribute.TriggerAttribute = null;
                    attribute.Channel = null;
                    attribute.AliasAsset = null;
                    attribute.AliasAttribute = null;
                    attribute.Value = null;
                    break;
                case AttributeTypeConstants.TYPE_INTEGRATION:
                    attribute.EnabledExpression = null;
                    attribute.Expression = null;
                    attribute.TriggerAttribute = null;
                    attribute.AliasAsset = null;
                    attribute.AliasAttribute = null;
                    attribute.Value = null;
                    break;
                case AttributeTypeConstants.TYPE_RUNTIME:
                    attribute.DeviceId = null;
                    attribute.Metric = null;
                    attribute.Channel = null;
                    attribute.AliasAsset = null;
                    attribute.AliasAttribute = null;
                    attribute.Value = null;
                    break;
            }
        }

        private DbConnection GetDbConnection()
        {
            var connectionString = _configuration["ConnectionStrings:Default"].BuildConnectionString(_configuration, _tenantContext.ProjectId);
            return new NpgsqlConnection(connectionString);
        }
    }

    class UomDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
    }

    class IntegrationDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    class IntegrationDeviceDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    class IntegrationMetricDto
    {
        public string Name { get; set; }
    }
}
