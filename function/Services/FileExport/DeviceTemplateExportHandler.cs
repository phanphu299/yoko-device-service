using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.Model.ExportModel;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.Export.Builder;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using AHI.Infrastructure.Service.Tag.Model;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using Dapper;
using Function.Extension;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;

namespace AHI.Device.Function.Service
{
    public class DeviceTemplateExportHandler : IExportHandler
    {
        private const string MODEL_NAME = "Template";
        private readonly IStorageService _storageService;
        private readonly IParserContext _context;
        private readonly JsonExportBuilder<DeviceTemplate> _builder;
        private readonly IReadOnlyDbConnectionFactory _readOnlyDbConnectionFactory;
        private readonly ITagService _tagService;

        public DeviceTemplateExportHandler(IStorageService storageService,
            IParserContext context,
            JsonExportBuilder<DeviceTemplate> builder,
            IReadOnlyDbConnectionFactory readOnlyDbConnectionFactory,
            ITagService tagService)
        {
            _storageService = storageService;
            _context = context;
            _builder = builder;
            _readOnlyDbConnectionFactory = readOnlyDbConnectionFactory;
            _tagService = tagService;
        }

        public async Task<string> HandleAsync(string workingFolder, IEnumerable<string> ids)
        {
            var templates = await GetTemplatesAsync(ids);
            return await HandleUploadContentAsync(templates);
        }

        private async Task<IEnumerable<DeviceTemplate>> GetTemplatesAsync(IEnumerable<string> ids)
        {
            IEnumerable<DeviceTemplate> templates;
            var query = BuildQueries(ids);
            using (var dbConnection = _readOnlyDbConnectionFactory.CreateConnection())
            {
                var queryResult = await dbConnection.QueryMultipleAsync(query, commandTimeout: 600);
                templates = TemplateQueryResult.ReadQueryResult(queryResult);
                await dbConnection.CloseAsync();
            }

            if (!templates.Any())
                throw new InvalidOperationException(ValidationMessage.EXPORT_NOT_FOUND);

            var details = ParseAggregateExpression(templates as TemplateQueryResult);

            foreach (var detail in details)
            {
                (templates as TemplateQueryResult).AddDetail(detail);
            }

            foreach (var template in templates)
            {
                foreach (var detail in template.Payloads.SelectMany(payload => payload.Details))
                {
                    if (detail.KeyType == TemplateKeyTypes.DEVICEID)
                        detail.DataType = DataTypeConstants.TYPE_DEVICEID;
                    if (detail.KeyType == TemplateKeyTypes.AGGREGATION)
                        detail.KeyType = TemplateKeyTypes.CALCULATION;
                }
            }

            await PopulateTagsAsync(templates);
            return templates;
        }

        private string BuildQueries(IEnumerable<string> ids)
        {
            var exportedIds = ids.Distinct().Select(id => $"'{id}'::uuid");
            var paramString = $"({string.Join(',', exportedIds)})";

            var templateQuery = $"SELECT id, name FROM {DbName.Table.DEVICE_TEMPLATE} WHERE id IN {paramString};";

            var payloadQuery = $@"SELECT
                                    template_payloads.id,
                                    template_payloads.device_template_id AS {nameof(TemplatePayload.TemplateId)},
                                    template_payloads.json_payload AS {nameof(TemplatePayload.JsonPayload)},
                                    template_details.id,
                                    template_details.template_payload_id AS {nameof(TemplateDetailDto.TemplatePayloadId)},
                                    template_details.key,
                                    template_details.name,
                                    template_details.expression,
                                    template_details.detail_id as {nameof(TemplateDetailDto.DetailId)},
                                    template_details.enabled,
                                    template_key_types.id AS {nameof(TemplateDetailDto.KeyTypeId)},
                                    template_key_types.name AS {nameof(TemplateDetailDto.KeyType)},
                                    template_details.data_type AS {nameof(TemplateDetailDto.DataType)}
                                FROM template_payloads
                                JOIN template_details ON template_details.template_payload_id = template_payloads.id
                                JOIN template_key_types ON template_details.key_type_id = template_key_types.id
                                WHERE template_payloads.device_template_id IN {paramString}
                                ORDER BY template_payloads.id, template_details.id;";

            var bindingQuery = $@"SELECT 
                                    template_bindings.device_template_id AS {nameof(TemplateBinding.TemplateId)},
                                    template_bindings.key,
                                    template_bindings.default_value AS {nameof(TemplateBinding.DefaultValueString)},
                                    template_bindings.data_type AS {nameof(TemplateBinding.DataType)}
                                FROM template_bindings
                                WHERE template_bindings.device_template_id IN {paramString}
                                ORDER BY 
                                    template_bindings.device_template_id, 
                                    template_bindings.id;";

            var tagsQuery = $@"select tag_id as TagId, entity_id_uuid as EntityIdGuid 
                                from {DbName.Table.ENTITY_TAG} 
                                where entity_id_uuid IN {paramString} AND entity_type = 'device_template';";

            return string.Concat(templateQuery, payloadQuery, bindingQuery, tagsQuery);
        }

        private async Task<string> HandleUploadContentAsync(IEnumerable<DeviceTemplate> templates)
        {
            var timezone_offset = GetTimeZoneInfo();

            _builder.SetZipEntryNameBuilder(x =>
            {
                var timestamp = DateTime.UtcNow.ToTimestamp(timezone_offset);
                return x.Name.CreateJsonFileName(string.Empty, timestamp, MODEL_NAME.Length);
            });

            byte[] data = _builder.BuildContent(templates);

            string fileTimestamp = DateTime.UtcNow.ToTimestamp(timezone_offset);
            string fileName;
            if (templates.Count() == 1)
            {
                var template = templates.First();
                fileName = template.Name.CreateJsonFileName(string.Empty, fileTimestamp);
            }
            else
            {
                fileName = MODEL_NAME.CreateZipFileName(fileTimestamp);
            }

            var uniqueFilePath = $"{StorageConstants.DefaultExportPath}/{Guid.NewGuid():N}";
            return await _storageService.UploadAsync(uniqueFilePath, fileName, data);
        }

        private string GetTimeZoneInfo()
        {
            var timezone_offset = _context.GetContextFormat(ContextFormatKey.DATETIMEOFFSET) ?? DateTimeExtensions.DEFAULT_DATETIME_OFFSET;
            return DateTimeExtensions.ToValidOffset(timezone_offset);
        }

        private IEnumerable<TemplateDetail> ParseAggregateExpression(TemplateQueryResult templates)
        {
            var builder = new StringBuilder();
            var result = new List<TemplateDetail>();
            var details = templates.GetListDetails();
            foreach (var detail in details)
            {
                if (detail.KeyType == TemplateKeyTypes.AGGREGATION)
                {
                    detail.Expression = detail.Expression.Replace("${", RegexConstants.EXPRESSION_REFER_OPEN).Replace("}$", RegexConstants.EXPRESSION_REFER_CLOSE).Trim();
                    builder.Clear().Append(detail.Expression);
                    var match = Regex.Match(detail.Expression, RegexConstants.DEVICE_TEMPLATE_EXPRESSION_PATTERN, RegexOptions.IgnoreCase);
                    while (match.Success)
                    {
                        var detailId = match.Value.Replace(RegexConstants.EXPRESSION_REFER_OPEN, "").Replace(RegexConstants.EXPRESSION_REFER_CLOSE, "").Trim();
                        var key = details.FirstOrDefault(x => x.DetailId == detailId.ToGuid())?.Key;
                        builder.Replace(detailId, key);
                        match = match.NextMatch();
                    }
                    detail.Expression = builder.ToString();
                }
                result.Add(TemplateDetailDto.ConvertToTemplateDetail(detail));
            }
            return result;
        }

        private async Task PopulateTagsAsync(IEnumerable<DeviceTemplate> deviceTemplates)
        {
            var tagIds = deviceTemplates.SelectMany(d => d.Tags.Select(t => t.TagId)).Distinct();
            var tags = await _tagService.FetchTagsAsync(tagIds);

            foreach (var deviceTemplate in deviceTemplates.Where(d => d.Tags.Any()))
            {
                foreach (Function.Model.ImportExportTagDto tag in deviceTemplate.Tags)
                {
                    TagDto tagDto = tags.First(x => x.Id == tag.TagId);
                    tag.Key = tagDto.Key;
                    tag.Value = tagDto.Value;
                }
            }
        }
    }

    class TemplateQueryResult : IEnumerable<DeviceTemplate>
    {
        private readonly IDictionary<Guid, DeviceTemplate> _templates;
        private readonly IDictionary<int, TemplatePayload> _payloads;
        public IEnumerable<TemplateDetailDto> _detailDtos;

        private TemplateQueryResult()
        {
            _templates = new Dictionary<Guid, DeviceTemplate>();
            _payloads = new Dictionary<int, TemplatePayload>();
        }

        public TemplateQueryResult AddTemplate(DeviceTemplate template)
        {
            if (!_templates.ContainsKey(template.Id))
                _templates[template.Id] = template;

            return this;
        }

        public TemplateQueryResult AddPayload(TemplatePayload payload)
        {
            if (!_payloads.ContainsKey(payload.Id))
            {
                _templates[payload.TemplateId].Payloads.Add(payload);
                _payloads[payload.Id] = payload;
            }
            return this;
        }

        public TemplateQueryResult AddDetail(TemplateDetail detail)
        {
            _payloads[detail.TemplatePayloadId].Details.Add(detail);
            return this;
        }

        public TemplateQueryResult AddBinding(TemplateBinding binding)
        {
            _templates[binding.TemplateId].Bindings.Add(binding);
            return this;
        }

        public TemplateQueryResult AddTag(Guid templateId, long tagId)
        {
            _templates[templateId].Tags.Add(new Function.Model.ImportExportTagDto() { TagId = tagId });
            return this;
        }

        public IEnumerator<DeviceTemplate> GetEnumerator()
        {
            return _templates.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _templates.Values.GetEnumerator();
        }

        public static TemplateQueryResult ReadQueryResult(SqlMapper.GridReader queryResult)
        {
            var result = new TemplateQueryResult();
            var detailDtos = new List<TemplateDetailDto>();

            queryResult.Read<DeviceTemplate>(new[] { typeof(DeviceTemplate) }, @params =>
            {
                var template = @params[0] as DeviceTemplate;
                result.AddTemplate(template);
                return null;
            });

            queryResult.Read<TemplatePayload>(new[] { typeof(TemplatePayload), typeof(TemplateDetailDto) }, @params =>
            {
                var payload = @params[0] as TemplatePayload;
                var detail = @params[1] as TemplateDetailDto;
                detailDtos.Add(detail);
                result.AddPayload(payload);
                return null;
            });

            queryResult.Read<TemplateBinding>(new[] { typeof(TemplateBinding) }, @params =>
            {
                var binding = @params[0] as TemplateBinding;
                binding.ConvertDefaultValue();
                result.AddBinding(binding);
                return null;
            });

            queryResult.Read<EntityTag>(new[] { typeof(EntityTag) }, @params =>
            {
                var entityTag = @params[0] as EntityTag;
                result.AddTag(entityTag.EntityIdGuid.Value, entityTag.TagId);
                return null;
            });


            result._detailDtos = detailDtos;
            return result;
        }

        public IEnumerable<TemplateDetailDto> GetListDetails()
        {
            return _detailDtos;
        }
    }
}