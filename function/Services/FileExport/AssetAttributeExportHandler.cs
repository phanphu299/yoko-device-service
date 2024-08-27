using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Dapper;
using System.Linq;
using System;
using System.IO;
using System.Data;
using Function.Extension;
using AHI.Device.Function.Model.ExportModel;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.Constant;
using AHI.Infrastructure.Export.Builder;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Function.Model.SearchModel;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using AHI.Infrastructure.MultiTenancy.Extension;
using System.Globalization;
using System.Collections;
using AHI.Device.Function.FileParser.Template.Constant;

namespace AHI.Device.Function.Service
{
    public class AssetAttributeExportHandler : IExportHandler
    {
        private readonly ITenantContext _tenantContext;
        private readonly IParserContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ExcelExportBuider _excelExportBuidler;
        private readonly IStorageService _storageService;
        private readonly Regex _expressionVariableRegex = new Regex(@"\${([-abcdef\d]{32,36})}\$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public AssetAttributeExportHandler(
            ITenantContext tenantContext, IParserContext parserContext,
            IConfiguration configuration, IHttpClientFactory httpClientFactory,
            ExcelExportBuider excelExportBuidler,
            IStorageService storageService)
        {
            _tenantContext = tenantContext;
            _context = parserContext;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _excelExportBuidler = excelExportBuidler;
            _storageService = storageService;
        }

        public async Task<string> HandleAsync(string workingFolder, IEnumerable<string> ids)
        {
            var timezone_offset = GetTimeZoneInfo();
            var path = Path.Combine(workingFolder, "AppData", "ExportTemplate", "AssetAttribute.xlsx");

            _excelExportBuidler.SetTemplate(path);

            var asset = await GetAssetAttributesAsync(ids.FirstOrDefault());
            _excelExportBuidler.SetData<AttributeModel>(
                sheetName: TemplateSheetName.DEFAULT_SHEET_NAME,
                data: new List<AttributeModel>(asset.Attributes)
            );

            var fileName = $"{asset.Name}_Attributes_{DateTime.UtcNow.ToTimestamp(timezone_offset)}.xlsx";

            var uniqueFilePath = $"{StorageConstants.DefaultExportPath}/{Guid.NewGuid():N}";
            return await _storageService.UploadAsync(uniqueFilePath, fileName, _excelExportBuidler.BuildExcelStream());
        }

        private async Task<AssetAttribute> GetAssetAttributesAsync(string assetId)
        {
            var query = BuildQueries();
            IEnumerable<AssetAttribute> assetAttributeData;
            var connectionString = _configuration["ConnectionStrings:Default"].BuildConnectionString(_configuration, _tenantContext.ProjectId);
            using (var dbConnection = new NpgsqlConnection(connectionString))
            {
                var queryResult = await dbConnection.QueryMultipleAsync(query, new { AssetId = Guid.Parse(assetId) }, commandTimeout: 600);
                assetAttributeData = AssetAttributeQueryResult.ReadQueryResult(queryResult, _context);

                await dbConnection.CloseAsync();
            }

            if (!assetAttributeData.Any())
                throw new InvalidOperationException(ValidationMessage.EXPORT_NOT_FOUND);
            var assetAttributes = assetAttributeData.FirstOrDefault();
            assetAttributes.Attributes = assetAttributes.Attributes.OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber).ToList();

            await QueryChannelAsync(assetAttributes);
            ParseRuntimeExpression(assetAttributes);
            return assetAttributes;
        }

        private string BuildQueries()
        {
            var asset = @$"SELECT a.id, a.name
                            FROM assets a
                            WHERE a.id = @AssetId;";

            var assetAttribute = @$"SELECT
                                        att.id AS {nameof(AttributeModel.Id)},
                                        a.id AS {nameof(AttributeModel.AssetId)},
                                        att.name AS {nameof(AttributeModel.AttributeName)},
                                        att.attribute_type AS {nameof(AttributeModel.Type)},
                                        attint.integration_id AS {nameof(AttributeModel.ChannelId)},
                                        CASE
                                            WHEN att.attribute_type = 'dynamic' THEN attdym.device_id
                                            WHEN att.attribute_type = 'command' THEN attcmd.device_id
                                            WHEN att.attribute_type = 'integration' THEN attint.device_id
                                        END AS {nameof(AttributeModel.DeviceId)},
                                        -- https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/58582
                                        -- According to the AC of this US, the Metric will be exported as MetricName for the 'dynamic' type and MetricKey for the 'command' type.
                                        CASE
                                            WHEN att.attribute_type = 'dynamic' THEN detail.name
                                            WHEN att.attribute_type = 'command' THEN attcmd.metric_key
                                            WHEN att.attribute_type = 'integration' THEN attint.metric_key
                                        END AS {nameof(AttributeModel.Metric)},
                                        att.value AS {nameof(AttributeModel.Value)},
                                        att.data_type AS {nameof(AttributeModel.DataType)},
                                        CASE
                                            WHEN att.attribute_type = 'alias' THEN alias_uom.abbreviation
                                            ELSE uom.abbreviation
                                        END AS {nameof(AttributeModel.Uom)},
                                        attals.alias_asset_id AS {nameof(AttributeModel.AliasAsset)},
                                        attalias.name AS {nameof(AttributeModel.AliasAttribute)},
                                        attrt.expression {nameof(AttributeModel.Expression)},
                                        attrt.enabled_expression AS {nameof(AttributeModel.EnabledExpression)},
                                        atttg.name AS {nameof(AttributeModel.TriggerAttribute)},
                                        att.decimal_place as {nameof(AttributeModel.DecimalPlace)},
                                        att.thousand_separator as {nameof(AttributeModel.ThousandSeparator)},
                                        att.created_utc as {nameof(AttributeModel.CreatedUtc)},
                                        att.sequential_number as {nameof(AttributeModel.SequentialNumber)}
                                    FROM assets a
                                    LEFT JOIN asset_attributes att ON a.id = att.asset_id
                                    -- Dynamic
                                    LEFT JOIN asset_attribute_dynamic attdym ON attdym.asset_attribute_id = att.id AND att.attribute_type = 'dynamic'
                                    -- Command
                                    LEFT JOIN asset_attribute_commands attcmd ON attcmd.asset_attribute_id = att.id AND att.attribute_type = 'command'
                                    -- Runtime
                                    LEFT JOIN asset_attribute_runtimes attrt ON attrt.asset_attribute_id = att.id AND att.attribute_type = 'runtime'
                                    -- Alias
                                    LEFT JOIN asset_attribute_alias attals ON attals.asset_attribute_id = att.id AND att.attribute_type = 'alias'
                                    -- Alias Target Attribute (alias target attribute id can be attribute id, or attribute mapping id)
                                    LEFT JOIN
                                        (SELECT asset_attribute_all_mapping.id, asset_attribute_templates.name, asset_attribute_templates.uom_id
                                        FROM
                                            (SELECT id, asset_attribute_template_id FROM asset_attribute_static_mapping
                                            UNION
                                            SELECT id, asset_attribute_template_id FROM asset_attribute_dynamic_mapping
                                            UNION
                                            SELECT id, asset_attribute_template_id FROM asset_attribute_runtime_mapping
                                            UNION
                                            SELECT id, asset_attribute_template_id FROM asset_attribute_command_mapping
                                            UNION
                                            SELECT id, asset_attribute_template_id FROM asset_attribute_alias_mapping) asset_attribute_all_mapping
                                            JOIN asset_attribute_templates ON asset_attribute_all_mapping.asset_attribute_template_id = asset_attribute_templates.id
                                        UNION
                                        SELECT id, name, uom_id FROM asset_attributes) attalias ON attals.alias_attribute_id = attalias.id
                                    -- Integration
                                    LEFT JOIN asset_attribute_integration attint ON attint.asset_attribute_id = att.id AND att.attribute_type = 'integration'
                                    -- Trigger asset and trigger attribute
                                    LEFT JOIN asset_attribute_runtime_triggers atttgr ON attrt.asset_attribute_id = atttgr.attribute_id AND attrt.is_trigger_visibility AND atttgr.is_selected
                                    LEFT JOIN asset_attributes atttg ON atttgr.trigger_attribute_id = atttg.id
                                    -- Device metric
                                    LEFT JOIN devices dv ON attdym.device_id = dv.id
                                    LEFT JOIN device_templates tpl ON dv.device_template_id = tpl.id
                                    LEFT JOIN template_details detail ON detail.template_payload_id IN (SELECT payload.id FROM template_payloads payload WHERE payload.device_template_id = tpl.id) AND detail.key = attdym.metric_key
                                    -- Uoms
                                    LEFT JOIN uoms uom ON att.uom_id = uom.id
                                    LEFT JOIN uoms alias_uom ON attalias.uom_id = alias_uom.id
                                    WHERE a.id = @AssetId;";

            var assetAttributeTemplate = @$"SELECT
                                            CASE
                                                WHEN att.attribute_type = 'static' THEN attstamp.id
                                                WHEN att.attribute_type = 'alias' THEN attalsm.id
                                                WHEN att.attribute_type = 'dynamic' THEN attdymp.id
                                                WHEN att.attribute_type = 'command' THEN attcmdm.id
                                                WHEN att.attribute_type = 'runtime' THEN attrtm.id
                                                WHEN att.attribute_type = 'integration' THEN attintm.id
                                            END AS {nameof(AttributeModel.Id)},
                                            a.id AS {nameof(AttributeModel.AssetId)},
                                            att.name AS {nameof(AttributeModel.AttributeName)},
                                            att.attribute_type AS {nameof(AttributeModel.Type)},
                                            attint.integration_id AS {nameof(AttributeModel.ChannelId)},
                                            CASE
                                                WHEN att.attribute_type = 'dynamic' THEN attdymp.device_id
                                                WHEN att.attribute_type = 'command' THEN attcmdm.device_id
                                                WHEN att.attribute_type = 'integration' THEN attint.device_id
                                            END AS {nameof(AttributeModel.DeviceId)},
                                            -- https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/58582
                                            -- According to the AC of this US, the Metric will be exported as MetricName for the 'dynamic' type and MetricKey for the 'command' type.
                                            CASE
                                                WHEN att.attribute_type = 'dynamic' THEN detail.name
                                                WHEN att.attribute_type = 'command' THEN attcmd.metric_key
                                                WHEN att.attribute_type = 'integration' THEN attint.metric_key
                                            END AS {nameof(AttributeModel.Metric)},
                                            att.data_type AS {nameof(AttributeModel.DataType)},
                                            att.value AS {nameof(AttributeModel.Value)},
                                            CASE
                                                WHEN att.attribute_type = 'alias' THEN alias_uom.abbreviation
                                                ELSE uom.abbreviation
                                            END AS {nameof(AttributeModel.Uom)},
                                            attalsm.alias_asset_id AS {nameof(AttributeModel.AliasAsset)},
                                            attalias.name AS {nameof(AttributeModel.AliasAttribute)},
                                            attrtm.expression {nameof(AttributeModel.Expression)},
                                            attrtm.enabled_expression AS {nameof(AttributeModel.EnabledExpression)},
                                            atttg.name AS {nameof(AttributeModel.TriggerAttribute)},
                                            att.decimal_place as {nameof(AttributeModel.DecimalPlace)},
                                            att.thousand_separator as {nameof(AttributeModel.ThousandSeparator)},
                                            att.created_utc as {nameof(AttributeModel.CreatedUtc)},
                                            att.sequential_number as {nameof(AttributeModel.SequentialNumber)}
                                        FROM assets a
                                        LEFT JOIN asset_templates atmp ON atmp.id = a.asset_template_id
                                        LEFT JOIN asset_attribute_templates att ON att.asset_template_id = atmp.id
                                        -- Static
                                        LEFT JOIN asset_attribute_static_mapping attstamp ON attstamp.asset_attribute_template_id = att.id AND attstamp.asset_id = @AssetId
                                        -- Dynamic
                                        LEFT JOIN asset_attribute_template_dynamics attdym ON attdym.asset_attribute_template_id = att.id AND att.attribute_type = 'dynamic'
                                        LEFT JOIN asset_attribute_dynamic_mapping attdymp ON attdymp.asset_attribute_template_id = att.id AND attdymp.asset_id = @AssetId
                                        -- Command
                                        LEFT JOIN asset_attribute_template_commands attcmd ON attcmd.asset_attribute_template_id = att.id AND att.attribute_type = 'command'
                                        LEFT JOIN asset_attribute_command_mapping attcmdm ON attcmdm.asset_attribute_template_id = att.id AND attcmdm.asset_id = @AssetId
                                        -- Runtime
                                        LEFT JOIN asset_attribute_template_runtimes attrt ON attrt.asset_attribute_template_id = att.id AND att.attribute_type = 'runtime'
                                        LEFT JOIN asset_attribute_runtime_mapping attrtm ON attrtm.asset_attribute_template_id = att.id AND attrtm.asset_id = @AssetId
                                        -- Alias
                                        LEFT JOIN asset_attribute_alias_mapping attalsm ON attalsm.asset_attribute_template_id = att.id AND att.attribute_type = 'alias' AND attalsm.asset_id = @AssetId
                                        -- Target Alias Attribute (alias target attribute id can be attribute id, or attribute mapping id)
                                        LEFT JOIN
                                            (SELECT asset_attribute_all_mapping.id, asset_attribute_templates.name, asset_attribute_templates.uom_id
                                            FROM
                                                (SELECT id, asset_attribute_template_id FROM asset_attribute_static_mapping
                                                UNION
                                                SELECT id, asset_attribute_template_id FROM asset_attribute_dynamic_mapping
                                                UNION
                                                SELECT id, asset_attribute_template_id FROM asset_attribute_runtime_mapping
                                                UNION
                                                SELECT id, asset_attribute_template_id FROM asset_attribute_command_mapping
                                                UNION
                                                SELECT id, asset_attribute_template_id FROM asset_attribute_alias_mapping) asset_attribute_all_mapping
                                                JOIN asset_attribute_templates ON asset_attribute_all_mapping.asset_attribute_template_id = asset_attribute_templates.id
                                            UNION
                                            SELECT id, name, uom_id FROM asset_attributes) attalias ON attalsm.alias_attribute_id = attalias.id
                                        -- Integration
                                        LEFT JOIN asset_attribute_template_integrations attint ON attint.asset_attribute_template_id = att.id AND att.attribute_type = 'integration'
                                        LEFT JOIN asset_attribute_integration_mapping attintm ON attintm.asset_attribute_template_id = att.id AND attintm.asset_id = @AssetId
                                        -- Trigger asset template and trigger attribute
                                        LEFT JOIN asset_attribute_templates atttg ON attrt.trigger_attribute_id = atttg.id
                                        -- Device metric
                                        LEFT JOIN device_templates tpl ON attdym.device_template_id = tpl.id
                                        LEFT JOIN template_details detail ON detail.template_payload_id IN (SELECT payload.id FROM template_payloads payload WHERE payload.device_template_id = tpl.id) AND detail.key = attdym.metric_key
                                        -- Uoms
                                        LEFT JOIN uoms uom ON att.uom_id = uom.id
                                        LEFT JOIN uoms alias_uom ON attalias.uom_id = alias_uom.id
                                        WHERE a.id = @AssetId;";

            return string.Concat(asset, assetAttribute, assetAttributeTemplate);
        }

        private async Task QueryChannelAsync(AssetAttribute assetAttributes)
        {
            var integrationAttributes = assetAttributes.Attributes.Where(attribute => attribute.IsIntegrationAttribute);
            if (!integrationAttributes.Any())
                return;

            var channelIds = integrationAttributes.Select(x => x.ChannelId).Distinct();
            var searchQuery = new FilteredSearchQuery(
                FilteredSearchQuery.LogicalOp.Or,
                filterObjects: channelIds.Select(id => new SearchFilter("id", id.ToString(), queryType: "guid")).ToArray()
            );

            IDictionary<Guid, string> integrationInfos = null;
            try
            {
                var httpClient = _httpClientFactory.CreateClient(ClientNameConstant.BROKER_SERVICE, _tenantContext);
                var response = await httpClient.SearchAsync<IntegrationInfo>("bkr/integrations/search", searchQuery);

                integrationInfos = response.Data.ToDictionary(integration => integration.Id, integration => integration.Name);
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException($"Failed to get integrations.");
            }

            foreach (var attribute in integrationAttributes)
            {
                if (integrationInfos.TryGetValue(attribute.ChannelId.Value, out var channel))
                    attribute.Channel = channel;
            }
        }

        private void ParseRuntimeExpression(AssetAttribute assetAttribute)
        {
            var builder = new StringBuilder();
            foreach (var attribute in assetAttribute.Attributes.Where(a => a.IsRuntimeAttribute && a.EnabledExpression == true))
            {
                builder.Clear().Append(attribute.Expression);
                var matches = _expressionVariableRegex.Matches(attribute.Expression);
                if (matches.Count == 0)
                    continue;

                foreach (var match in matches.DistinctBy(m => m.Value))
                {
                    var id = match.Groups[1].Value.ToGuid();
                    var name = assetAttribute.Attributes.FirstOrDefault(x => x.Id == id)?.AttributeName;

                    builder.Replace(match.Value, $"{RegexConstants.EXPRESSION_REFER_OPEN}{name}{RegexConstants.EXPRESSION_REFER_CLOSE}");
                }
                attribute.Expression = builder.ToString();
            }
        }

        private string GetTimeZoneInfo()
        {
            var timezone_offset = _context.GetContextFormat(ContextFormatKey.DATETIMEOFFSET) ?? DateTimeExtensions.DEFAULT_DATETIME_OFFSET;
            return DateTimeExtensions.ToValidOffset(timezone_offset);
        }

        protected class IntegrationInfo
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
    }

    class AssetAttributeQueryResult : IEnumerable<AssetAttribute>
    {
        private readonly Dictionary<Guid, AssetAttribute> _assetAttributes;
        private AssetAttributeQueryResult()
        {
            _assetAttributes = new Dictionary<Guid, AssetAttribute>();
        }

        public AssetAttributeQueryResult AddAssetAttribute(AssetAttribute assetAttribute)
        {
            if (!_assetAttributes.ContainsKey(assetAttribute.Id))
                _assetAttributes[assetAttribute.Id] = assetAttribute;

            return this;
        }

        public AssetAttributeQueryResult AddAttribute(AttributeModel attribute)
        {
            _assetAttributes[attribute.AssetId].Attributes.Add(attribute);
            return this;
        }

        public IEnumerator<AssetAttribute> GetEnumerator()
        {
            return _assetAttributes.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _assetAttributes.Values.GetEnumerator();
        }

        public static AssetAttributeQueryResult ReadQueryResult(SqlMapper.GridReader queryResult, IParserContext context)
        {
            var result = new AssetAttributeQueryResult();

            queryResult.Read<AssetAttribute>(new[] { typeof(AssetAttribute) }, @param =>
            {
                var attribute = @param[0] as AssetAttribute;
                result.AddAssetAttribute(attribute);
                return null;
            });

            queryResult.Read<AttributeModel>(new[] { typeof(AttributeModel) }, @param =>
            {
                var attribute = @param[0] as AttributeModel;

                ParseDateTimeValue(attribute, context);
                if (attribute?.Id != Guid.Empty)
                    result.AddAttribute(attribute);
                return null;
            });

            queryResult.Read<AttributeModel>(new[] { typeof(AttributeModel) }, @param =>
            {
                var attribute = @param[0] as AttributeModel;

                ParseDateTimeValue(attribute, context);
                if (attribute?.Id != Guid.Empty)
                    result.AddAttribute(attribute);
                return null;
            });

            return result;
        }

        private static void ParseDateTimeValue(AttributeModel attribute, IParserContext context)
        {
            var dateTimeFormat = context.GetContextFormat(ContextFormatKey.DATETIMEFORMAT);
            var timezoneOffset = context.GetContextFormat(ContextFormatKey.DATETIMEOFFSET);
            var offset = TimeSpan.Parse(timezoneOffset);
            // parse the attribute base on user format and offset as well
            if (!string.IsNullOrEmpty(attribute.Value) && attribute.DataType == DataTypeConstants.TYPE_DATETIME)
            {
                var datetime = DateTime.ParseExact(attribute.Value, AHI.Infrastructure.SharedKernel.Extension.Constant.DefaultDateTimeFormat, CultureInfo.InvariantCulture);
                var datetimeOffsetValue = new DateTimeOffset(datetime, TimeSpan.Zero).ToOffset(offset);
                attribute.Value = datetimeOffsetValue.ToString(dateTimeFormat);
            }
        }
    }
}
