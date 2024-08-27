using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using AHI.Device.Function.Model.ExportModel;
using AHI.Device.Function.Service.Abstraction;
using Dapper;
using AHI.Infrastructure.Export.Builder;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.FileParser.Abstraction;
using Function.Extension;
using AHI.Device.Function.Model.SearchModel;
using AHI.Device.Function.Constant;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using AHI.Infrastructure.Service.Tag.Model;

namespace AHI.Device.Function.Service
{
    public class UomExportHandler : IExportHandler
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;
        private readonly ExcelExportBuider _excelExportBuidler;
        private readonly IStorageService _storageService;
        private readonly ITagService _tagService;
        private readonly IParserContext _context;
        private readonly IReadOnlyDbConnectionFactory _readOnlyDbConnectionFactory;

        public UomExportHandler(IConfiguration configuration,
            IHttpClientFactory factory,
            ITenantContext tenantContext,
            ExcelExportBuider excelExportBuidler,
            IStorageService storageService,
            ITagService tagService,
            IParserContext context,
            IReadOnlyDbConnectionFactory readOnlyDbConnectionFactory)
        {
            _excelExportBuidler = excelExportBuidler;
            _tenantContext = tenantContext;
            _storageService = storageService;
            _configuration = configuration;
            _httpClientFactory = factory;
            _tagService = tagService;
            _context = context;
            _readOnlyDbConnectionFactory = readOnlyDbConnectionFactory;
        }

        public async Task<string> HandleAsync(string workingFolder, IEnumerable<string> ids)
        {
            var timezone_offset = GetTimeZoneInfo();
            var path = Path.Combine(workingFolder, "AppData", "ExportTemplate", "Uom.xlsx");

            _excelExportBuidler.SetTemplate(path);

            var uoms = await GetUomAsync(ids);

            _excelExportBuidler.SetData<Uom>(
                sheetName: "UoMs",
                data: new List<Uom>(uoms)
            );

            var fileName = $"UoMs_{DateTime.UtcNow.ToTimestamp(timezone_offset)}.xlsx";

            var uniqueFilePath = $"{StorageConstants.DefaultExportPath}/{Guid.NewGuid():N}";
            return await _storageService.UploadAsync(uniqueFilePath, fileName, _excelExportBuidler.BuildExcelStream());
        }

        private async Task<IEnumerable<Uom>> GetUomAsync(IEnumerable<string> ids)
        {
            IEnumerable<Uom> uoms = Array.Empty<Uom>();
            IEnumerable<EntityTag> tags = Array.Empty<EntityTag>();
            using (var dbConnection = _readOnlyDbConnectionFactory.CreateConnection())
            {
                var query = @$"SELECT
                                    u.id as Id,
                                    u.name AS Name,
                                    u.lookup_code AS Lookup,
                                    u.ref_factor AS RefFactorValue,
                                    u.ref_offset AS RefOffsetValue,
                                    u.canonical_factor AS CanonicalFactorValue,
                                    u.canonical_offset AS CanonicalOffsetValue,
                                    u.abbreviation AS Abbreviation,
                                    u.ref_id AS RefId,
                                    u_ref.abbreviation AS RefName
                                FROM (SELECT * FROM uoms WHERE deleted = false) u
                                LEFT JOIN uoms u_ref ON u_ref.id = u.ref_id
                                JOIN (SELECT * FROM {BuildExportIdList(ids)} AS u_list(id, ordinal)) u_list ON u.id = u_list.id
                                ORDER BY u_list.ordinal;
                                
                                SELECT *
                                FROM entity_tags et
                                WHERE et.entity_type = '{nameof(Uom)}'
                                AND et.entity_id_int = ANY(@Ids)";

                var reader = await dbConnection.QueryMultipleAsync(query, new { Ids = ids.Select(int.Parse).ToArray() }, commandTimeout: 600);
                uoms = await reader.ReadAsync<Uom>();
                tags = await reader.ReadAsync<EntityTag>();
                await dbConnection.CloseAsync();
            }
            if (!uoms.Any())
                throw new InvalidOperationException(ValidationMessage.EXPORT_NOT_FOUND);

            await PopulateCategoryAsync(uoms);
            await PopulateTagsAsync(uoms, tags);
            return uoms;
        }

        private string BuildExportIdList(IEnumerable<string> ids)
        {
            var ordinal = 1;
            var exportedIds = ids.Distinct();
            return $"(VALUES {string.Join(',', exportedIds.Select(id => $"({id},{ordinal++})"))})";
        }

        private async Task PopulateCategoryAsync(IEnumerable<Uom> uoms)
        {
            var codes = uoms.Select(uom => uom.Lookup).Distinct();
            var query = new FilteredSearchQuery(
                FilteredSearchQuery.LogicalOp.Or,
                filterObjects: codes.Select(code => new SearchFilter("id", code)).ToArray()
            );

            IDictionary<string, string> categoryInfos;
            try
            {
                var httpClient = _httpClientFactory.CreateClient(ClientNameConstant.CONFIGURATION_SERVICE, _tenantContext);
                var response = await httpClient.SearchAsync<LookupInfo>("cnm/lookups/uoms/search", query);

                categoryInfos = response.Data.ToDictionary(info => info.Id, info => info.Name);
            }
            catch (HttpRequestException)
            {
                foreach (var uom in uoms)
                    uom.Lookup = "N/A";
                return;
            }

            foreach (var uom in uoms)
            {
                if (categoryInfos.TryGetValue(uom.Lookup, out var name))
                    uom.Lookup = name;
                else
                    uom.Lookup = "N/A";
            }
        }

        private async Task PopulateTagsAsync(IEnumerable<Uom> uoms, IEnumerable<EntityTag> entityTags)
        {
            var tagIds = entityTags.Select(x => x.TagId).Distinct();
            var tags = await _tagService.FetchTagsAsync(tagIds);

            foreach (var uom in uoms)
            {
                var uomTagIds = entityTags.Where(x => x.EntityIdInt == uom.Id)
                                          .OrderBy(x => x.Id)
                                          .Select(x => x.TagId)
                                          .ToList();
                uom.Tags = string.Join(TagConstants.TAG_IMPORT_EXPORT_SEPARATOR, tags.Where(x => uomTagIds.Contains(x.Id)).OrderBy(x => uomTagIds.IndexOf(x.Id)).Select(x => $"{x.Key} : {x.Value}"));
            }
        }

        private string GetTimeZoneInfo()
        {
            var timezone_offset = _context.GetContextFormat(ContextFormatKey.DATETIMEOFFSET) ?? DateTimeExtensions.DEFAULT_DATETIME_OFFSET;
            return DateTimeExtensions.ToValidOffset(timezone_offset);
        }
    }
    class LookupInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}

