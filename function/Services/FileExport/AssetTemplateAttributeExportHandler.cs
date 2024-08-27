using System.Threading.Tasks;
using System.Collections.Generic;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Dapper;
using AHI.Device.Function.Model.ExportModel;
using System.Linq;
using System;
using System.IO;
using AHI.Infrastructure.Export.Builder;
using Function.Extension;
using System.Text.RegularExpressions;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.Constant;
using System.Text;
using AHI.Device.Function.Model.SearchModel;
using System.Net.Http;
using AHI.Device.Function.Constant;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.MultiTenancy.Extension;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using System.Globalization;
using AHI.Device.Function.FileParser.Template.Constant;

namespace AHI.Device.Function.Service
{
    public class AssetTemplateAttributeExportHandler : IExportHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        private readonly ExcelExportBuider _excelExportBuidler;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IStorageService _storageService;
        private readonly IParserContext _context;
        private readonly Regex _expressionVariableRegex = new Regex(@"\${([-abcdef\d]{32,36})}\$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public AssetTemplateAttributeExportHandler(IConfiguration configuration, ITenantContext tenantContext,
                                          ExcelExportBuider excelExportBuidler, IHttpClientFactory httpClientFactory,
                                          IStorageService storageService, IParserContext context)
        {
            _excelExportBuidler = excelExportBuidler;
            _tenantContext = tenantContext;
            _httpClientFactory = httpClientFactory;
            _storageService = storageService;
            _configuration = configuration;
            _context = context;
        }

        public async Task<string> HandleAsync(string workingFolder, IEnumerable<string> ids)
        {
            var timezone_offset = GetTimeZoneInfo();
            var path = Path.Combine(workingFolder, "AppData", "ExportTemplate", "AssetTemplateAttribute.xlsx");

            _excelExportBuidler.SetTemplate(path);

            string assetTemplateName = "AssetTemplate";
            var data = new List<AssetTemplateAttribute>();
            if (ids.Any() && ids.First() != Guid.Empty.ToString())
            {
                var assetTemplate = await GetAssetTemplateAttributeAsync(ids.First());

                data.AddRange(assetTemplate.Attributes);
                assetTemplateName = assetTemplate.Name;
            }

            _excelExportBuidler.SetData<AssetTemplateAttribute>(
                sheetName: TemplateSheetName.DEFAULT_SHEET_NAME,
                data: new List<AssetTemplateAttribute>(data)
            );
            var fileName = $"{assetTemplateName}_Attributes_{DateTime.UtcNow.ToTimestamp(timezone_offset)}.xlsx";
            var uniqueFilePath = $"{StorageConstants.DefaultExportPath}/{Guid.NewGuid():N}";
            return await _storageService.UploadAsync(uniqueFilePath, fileName, _excelExportBuidler.BuildExcelStream());
        }

        private async Task<AssetTemplate> GetAssetTemplateAttributeAsync(string templateId)
        {
            var templateData = new Dictionary<Guid, AssetTemplate>();
            var query = BuildQueries();
            var dateTimeFormat = _context.GetContextFormat(ContextFormatKey.DATETIMEFORMAT);
            var timezoneOffset = _context.GetContextFormat(ContextFormatKey.DATETIMEOFFSET);
            var offset = TimeSpan.Parse(timezoneOffset);
            var connectionString = _configuration["ConnectionStrings:Default"].BuildConnectionString(_configuration, _tenantContext.ProjectId);
            using (var dbConnection = new NpgsqlConnection(connectionString))
            {
                await dbConnection.QueryAsync<AssetTemplate, AssetTemplateAttribute, AssetTemplate>(
                    query,
                    (template, attribute) =>
                    {
                        if (!templateData.ContainsKey(template.Id))
                        {
                            template.Attributes = new List<AssetTemplateAttribute>();
                            templateData[template.Id] = template;
                        }
                        if (attribute != null)
                        {
                            // parse the attribute base on user format and offset as well
                            if (!string.IsNullOrEmpty(attribute.Value) && attribute.DataType == DataTypeConstants.TYPE_DATETIME)
                            {
                                var datetime = DateTime.ParseExact(attribute.Value, AHI.Infrastructure.SharedKernel.Extension.Constant.DefaultDateTimeFormat, CultureInfo.InvariantCulture);
                                var datetimeOffsetValue = new DateTimeOffset(datetime, TimeSpan.Zero).ToOffset(offset);
                                attribute.Value = datetimeOffsetValue.ToString(dateTimeFormat);
                            }
                            if (templateData.TryGetValue(template.Id, out var existingTemplate))
                            {
                                existingTemplate.Attributes.Add(attribute);
                            }
                        }

                        return template;
                    }, new { TemplateId = templateId }, commandTimeout: 600);

                await dbConnection.CloseAsync();
            }

            if (!templateData.Any())
                throw new InvalidOperationException(ValidationMessage.EXPORT_NOT_FOUND);

            var template = templateData.Values.First();
            await QueryChannelAsync(template);
            ParseRuntimeExpression(template);
            return template;
        }

        private string BuildQueries()
        {
            return @$"SELECT
                        atmp.id, atmp.name,
                        att.id, att.name AS {nameof(AssetTemplateAttribute.AttributeName)},
                        att.attribute_type AS {nameof(AssetTemplateAttribute.Type)},
                        CASE
                            WHEN att.attribute_type = 'integration' THEN attint.device_id
                            ELSE tpl.name
                        END AS {nameof(AssetTemplateAttribute.DeviceTemplate)},
                        attint.integration_id AS {nameof(AssetTemplateAttribute.ChannelId)},
                        attint.integration_markup_name AS {nameof(AssetTemplateAttribute.ChannelMarkup)},
                        -- attint.device_id AS {nameof(AssetTemplateAttribute.Device)},
                        CASE
                            WHEN att.attribute_type = 'dynamic' THEN attdym.markup_name
                            WHEN att.attribute_type = 'command' THEN attcmd.markup_name
                            WHEN att.attribute_type = 'integration' THEN attint.device_markup_name
                        END AS {nameof(AssetTemplateAttribute.DeviceMarkup)},
                        -- https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/65773
                        -- From the AC of this US, the Metric will be exported as MetricName for the 'dynamic' type and MetricKey for the 'command' type.
                        CASE
                            WHEN att.attribute_type = 'dynamic' THEN td.name
                            WHEN att.attribute_type = 'command' THEN attcmd.metric_key
                            WHEN att.attribute_type = 'integration' THEN attint.metric_key
                        END AS {nameof(AssetTemplateAttribute.Metric)},
                        att.value AS {nameof(AssetTemplateAttribute.Value)},
                        att.data_type AS {nameof(AssetTemplateAttribute.DataType)},
                        uom.abbreviation AS {nameof(AssetTemplateAttribute.Uom)},
                        attrt.expression {nameof(AssetTemplateAttribute.Expression)},
                        attrt.enabled_expression AS {nameof(AssetTemplateAttribute.EnabledExpression)},
                        att.decimal_place as {nameof(AssetTemplateAttribute.DecimalPlace)},
                        att.thousand_separator as {nameof(AssetTemplateAttribute.ThousandSeparator)},
                        atttg.name AS {nameof(AssetTemplateAttribute.TriggerAssetAttribute)}
                    FROM asset_templates atmp
                    LEFT JOIN asset_attribute_templates att ON att.asset_template_id = atmp.id
                    LEFT JOIN asset_attribute_template_dynamics attdym ON attdym.asset_attribute_template_id = att.id AND att.attribute_type = 'dynamic'
                    LEFT JOIN asset_attribute_template_commands attcmd ON attcmd.asset_attribute_template_id = att.id AND att.attribute_type = 'command'

                    -- trigger asset template and trigger attribute
                    LEFT JOIN asset_attribute_template_runtimes attrt ON attrt.asset_attribute_template_id = att.id AND att.attribute_type = 'runtime'
                    LEFT JOIN asset_attribute_templates atttg ON attrt.trigger_attribute_id = atttg.id

                    -- device metric
                    LEFT JOIN device_templates tpl ON attdym.device_template_id = tpl.id OR attcmd.device_template_id = tpl.id
                    LEFT JOIN template_details td ON td.template_payload_id IN (SELECT tp.id FROM template_payloads tp WHERE tp.device_template_id = tpl.id) AND attdym.metric_key = td.key
                    LEFT JOIN asset_attribute_template_integrations attint ON attint.asset_attribute_template_id = att.id AND att.attribute_type = 'integration'
                    LEFT JOIN uoms uom ON att.uom_id = uom.id
                    WHERE atmp.id = (@TemplateId)::uuid
                    ORDER BY att.created_utc asc, att.sequential_number";
        }

        private async Task QueryChannelAsync(AssetTemplate template)
        {
            var integrationAttributes = template.Attributes.Where(attribute => attribute.Type == "integration");
            if (integrationAttributes.Count() == 0)
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

        private void ParseRuntimeExpression(AssetTemplate template)
        {
            var builder = new StringBuilder();
            foreach (var attribute in template.Attributes.Where(a => a.Type == AttributeTypeConstants.TYPE_RUNTIME && a.EnabledExpression == true))
            {
                builder.Clear().Append(attribute.Expression);
                var matches = _expressionVariableRegex.Matches(attribute.Expression);
                if (matches.Count == 0)
                    continue;

                foreach (var match in matches.DistinctBy(m => m.Value))
                {
                    var id = match.Groups[1].Value.ToGuid();
                    var name = template.Attributes.FirstOrDefault(x => x.Id == id)?.AttributeName;

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

        class IntegrationInfo
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
    }
}
