using System.Threading.Tasks;
using System.Collections.Generic;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Microsoft.Extensions.Configuration;
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
using AHI.Infrastructure.MultiTenancy.Extension;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using System.Globalization;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using AHI.Infrastructure.Repository;
using Microsoft.Extensions.Logging;

namespace AHI.Device.Function.Service
{
    public class AssetTemplateExportHandler : IExportHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        private readonly ExcelExportBuider _excelExportBuidler;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IStorageService _storageService;
        private readonly IParserContext _context;
        private readonly Regex _expressionVariableRegex = new Regex(@"\${([-abcdef\d]{32,36})}\$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly IReadOnlyDbConnectionFactory _readOnlyDbConnectionFactory;
        private readonly ITagService _tagService;
        private readonly ILogger<AssetTemplateExportHandler> _logger;

        public AssetTemplateExportHandler(IConfiguration configuration,
            ITenantContext tenantContext,
            ExcelExportBuider excelExportBuidler,
            IHttpClientFactory httpClientFactory,
            IStorageService storageService,
            IParserContext context,
            IReadOnlyDbConnectionFactory readOnlyDbConnectionFactory,
            ITagService tagService,
            ILogger<AssetTemplateExportHandler> logger)
        {
            _excelExportBuidler = excelExportBuidler;
            _tenantContext = tenantContext;
            _httpClientFactory = httpClientFactory;
            _storageService = storageService;
            _configuration = configuration;
            _context = context;
            _readOnlyDbConnectionFactory = readOnlyDbConnectionFactory;
            _tagService = tagService;
            _logger = logger;
        }

        public async Task<string> HandleAsync(string workingFolder, IEnumerable<string> ids)
        {
            var timezone_offset = GetTimeZoneInfo();
            var path = Path.Combine(workingFolder, "AppData", "ExportTemplate", "AssetTemplate.xlsx");

            _excelExportBuidler.SetTemplate(path);

            var assetTemplates = await GetAssetTemplatesAsync(ids);

            foreach (var assetTemplate in assetTemplates)
            {
                _excelExportBuidler.SetData<AssetTemplateAttribute>(
                    sheetName: assetTemplate.Name.NormalizeSheetName(),
                    data: new List<AssetTemplateAttribute>(assetTemplate.Attributes)
                );
                _excelExportBuidler.ShiftRowsFromTop(assetTemplate.Attributes.Count + 1, 1);
                _excelExportBuidler.SetVerticalData(assetTemplate.Tags);
            }

            var fileName = $"AssetTemplates_{DateTime.UtcNow.ToTimestamp(timezone_offset)}.xlsx";

            var uniqueFilePath = $"{StorageConstants.DefaultExportPath}/{Guid.NewGuid():N}";
            return await _storageService.UploadAsync(uniqueFilePath, fileName, _excelExportBuidler.BuildExcelStream());
        }

        private async Task<IEnumerable<AssetTemplate>> GetAssetTemplatesAsync(IEnumerable<string> ids)
        {
            var templateData = new Dictionary<Guid, AssetTemplate>();
            var query = BuildQueries(ids);
            var queryTag = BuildTagEntityQueries(ids);

            var dateTimeFormat = _context.GetContextFormat(ContextFormatKey.DATETIMEFORMAT);
            var timezoneOffset = _context.GetContextFormat(ContextFormatKey.DATETIMEOFFSET);
            var offset = TimeSpan.Parse(timezoneOffset);
            IEnumerable<AssetTemplateTagEntity> assetTemplateTagEntities = null;

            using (var dbConnection = _readOnlyDbConnectionFactory.CreateConnection())
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
                }, commandTimeout: 600);

                assetTemplateTagEntities = await dbConnection.QueryAsync<AssetTemplateTagEntity>(queryTag, commandTimeout: 600);

                await dbConnection.CloseAsync();
            }

            if (!templateData.Any())
                throw new InvalidOperationException(ValidationMessage.EXPORT_NOT_FOUND);

            var templates = templateData.Values;

            await ProcessAttachTags(templates, assetTemplateTagEntities);

            await QueryChannelAsync(templates);
            ParseRuntimeExpression(templates);
            return templates.OrderBy(x => x.Ordinal);
        }

        // can you rename this function to ProcessAttachTagsAsync?
        private async Task ProcessAttachTags(Dictionary<Guid, AssetTemplate>.ValueCollection templates, IEnumerable<AssetTemplateTagEntity> assetTemplateTagEntities)
        {
            // Get all tagId associated with the asset template
            var tagIds = assetTemplateTagEntities.Where(x => x.TagId.HasValue).Select(x => x.TagId.Value).Distinct();

            // Fetch all tags with the list of tagIds above
            var tags = await _tagService.FetchTagsAsync(tagIds.Distinct());

            // Validate if the tags is not empty
            if (tags.Any())
            {
                // Group the asset template tags by asset template id
                var groupAssetTemplateTags = assetTemplateTagEntities.GroupBy(x => x.Id);

                foreach (var template in templates)
                {
                    // Get the group of tags associated with the asset template
                    var groupTemplates = groupAssetTemplateTags.Where(x => x.Key == template.Id);

                    // Validate if the group of tags is not empty
                    if (!groupTemplates.Any())
                    {
                        continue;
                    }

                    // Get the tag group ids
                    var tagGroupIds = groupTemplates.SelectMany(d => d).Where(d => d.TagId.HasValue).Select(x => x.TagId.Value).Distinct();

                    // Build the tag binding
                    var bindingTags = string.Join(TagConstants.TAG_IMPORT_EXPORT_SEPARATOR, tags.Where(d => tagGroupIds.Contains(d.Id)).Select(x => $"{x.Key} : {x.Value}"));

                    // Attach the tags to the asset template
                    if (!template.Tags.ContainsKey("Tags"))
                        template.Tags.Add("Tags", bindingTags);
                }
            }
        }

        private string BuildTagEntityQueries(IEnumerable<string> ids)
        {
            return @$"SELECT
                     atmp.id, et.tag_id
                     FROM  {DbName.Table.ASSET_TEMPLATE} atmp
                     JOIN (SELECT * FROM {BuildExportIdList(ids)} AS atmp_list(id, ordinal)) atmp_list ON atmp_list.id = atmp.id
                     LEFT JOIN entity_tags et ON et.entity_id_uuid = atmp.id AND et.entity_type = '{nameof(AssetTemplate)}'
                     ORDER BY et.id";
        }

        private string BuildQueries(IEnumerable<string> ids)
        {
            return @$"SELECT
                        atmp.id, atmp.name, atmp_list.ordinal,
                        att.id, att.name AS {nameof(AssetTemplateAttribute.AttributeName)},
                        att.attribute_type AS {nameof(AssetTemplateAttribute.Type)},
                        tmp.name AS {nameof(AssetTemplateAttribute.DeviceTemplate)},
                        attint.integration_id AS {nameof(AssetTemplateAttribute.ChannelId)},
                        attint.integration_markup_name AS {nameof(AssetTemplateAttribute.ChannelMarkup)},
                        attint.device_id AS {nameof(AssetTemplateAttribute.Device)},
                        CASE
                            WHEN att.attribute_type = 'dynamic' THEN attdym.markup_name
                            WHEN att.attribute_type = 'command' THEN attcmd.markup_name
                            WHEN att.attribute_type = 'integration' THEN attint.device_markup_name
                        END AS {nameof(AssetTemplateAttribute.DeviceMarkup)},
                        CASE
                            WHEN att.attribute_type = 'dynamic' THEN attdym.metric_key
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
                    FROM {DbName.Table.ASSET_TEMPLATE} atmp
                    LEFT JOIN asset_attribute_templates att ON att.asset_template_id = atmp.id
                    LEFT JOIN asset_attribute_template_dynamics attdym ON attdym.asset_attribute_template_id = att.id AND att.attribute_type = 'dynamic'
                    LEFT JOIN asset_attribute_template_commands attcmd ON attcmd.asset_attribute_template_id = att.id AND att.attribute_type = 'command'

                    -- trigger asset template and trigger attribute
                    LEFT JOIN asset_attribute_template_runtimes attrt ON attrt.asset_attribute_template_id = att.id AND att.attribute_type = 'runtime'
                    LEFT JOIN asset_attribute_templates atttg ON attrt.trigger_attribute_id = atttg.id

                    -- device metric
                    LEFT JOIN {DbName.Table.DEVICE_TEMPLATE} tmp ON attdym.device_template_id = tmp.id OR attcmd.device_template_id = tmp.id
                    LEFT JOIN asset_attribute_template_integrations attint ON attint.asset_attribute_template_id = att.id AND att.attribute_type = 'integration'
                    LEFT JOIN uoms uom ON att.uom_id = uom.id
                    JOIN (SELECT * FROM {BuildExportIdList(ids)} AS atmp_list(id, ordinal)) atmp_list ON atmp_list.id = atmp.id
                    ORDER BY atmp.id, att.created_utc asc";
        }

        private string BuildExportIdList(IEnumerable<string> ids)
        {
            var ordinal = 1;
            return $"(VALUES {string.Join(',', ids.Distinct().Select(id => $"('{id}'::uuid,{ordinal++})"))})";
        }

        private async Task QueryChannelAsync(IEnumerable<AssetTemplate> templates)
        {
            var integrationAttributes = templates.SelectMany(template => template.Attributes)
                .Where(attribute => attribute.Type == "integration");
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

        private void ParseRuntimeExpression(IEnumerable<AssetTemplate> templates)
        {
            var builder = new StringBuilder();
            foreach (var template in templates)
            {
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