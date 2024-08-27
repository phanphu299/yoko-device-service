using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.BlockFunction.Model;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using Newtonsoft.Json;

namespace Device.Application.Service
{
    public class AssetTableService : IAssetTableService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;
        public AssetTableService(IHttpClientFactory httpClientFactory, ITenantContext tenantContext)
        {
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
        }

        public async Task<Guid?> FetchAssetTableAsync(Guid assetId, string tableName)
        {
            // TODO: Leverage advanced search filter to query for a specific asset table
            var assetTableClient = _httpClientFactory.CreateClient(HttpClientNames.ASSET_TABLE, _tenantContext);
            var searchResponse = await assetTableClient.GetByteArrayAsync($"tbl/tables/asset/{assetId}/tables");
            var tables = searchResponse.Deserialize<BaseSearchResponse<AssetTableDto>>();
            var table = tables.Data.FirstOrDefault(t => string.Equals(t.Name, tableName, StringComparison.InvariantCultureIgnoreCase));
            return table?.Id;
        }

        public async Task<Guid?> FetchAssetTableByIdAsync(Guid assetId, Guid tableId)
        {
            var assetTableClient = _httpClientFactory.CreateClient(HttpClientNames.ASSET_TABLE, _tenantContext);
            var response = await assetTableClient.GetAsync($"tbl/tables/asset/{assetId}/tables/{tableId}/fetch");
            var content = await response.Content.ReadAsByteArrayAsync();
            var table = content.Deserialize<AssetTableDto>();
            return table?.Id;
        }

        public async Task<IEnumerable<TargetConnector>> SearchAssetTableAsync(IEnumerable<Guid> assetIds)
        {
            var assetTableClient = _httpClientFactory.CreateClient(HttpClientNames.ASSET_TABLE, _tenantContext);

            var searchContent = JsonConvert.SerializeObject(new
            {
                Filter = JsonConvert.SerializeObject(new
                {
                    queryKey = "assetId",
                    operation = "in",
                    queryType = "guid",
                    queryValue = string.Join(",", assetIds)
                }),
                PageSize = int.MaxValue,
                PageIndex = 0
            });

            var searchResponse = await assetTableClient.PostAsync($"tbl/tablelist/search", new StringContent(searchContent, System.Text.Encoding.UTF8, "application/json"));
            searchResponse.EnsureSuccessStatusCode();
            var content = await searchResponse.Content.ReadAsByteArrayAsync();
            var body = content.Deserialize<BaseSearchResponse<TargetConnector>>();
            return body.Data;
        }
    }
    public class AssetTableDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid AssetId { get; set; }
    }
}
