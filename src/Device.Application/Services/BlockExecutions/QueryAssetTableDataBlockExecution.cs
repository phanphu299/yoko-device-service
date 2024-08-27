using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Device.Application.Enum;
using Device.Application.Constant;
using Device.Application.BlockFunction.Model;
using Device.Application.Service.Abstraction;
using Newtonsoft.Json;
using System.Net.Http;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using System.Text;
using System.Collections.Generic;
using Device.Application.Extension;

namespace Device.Application.Service
{
    public class QueryAssetTableDataBlockExecution : BaseBlockExecution
    {
        public QueryAssetTableDataBlockExecution(IBlockExecution next,
                                                    IServiceProvider serviceProvider) : base(next, serviceProvider)
        {
        }

        protected override BlockOperator CurrentBlockOperator => BlockOperator.QueryAssetTableData;

        protected override async Task<BlockQueryResult> ExecuteOperationAsync(IBlockContext context)
        {
            using (var scope = _serviceProvider.CreateNewScope())
            {
                var (assetId, tableId, queryCriteria) = await GetAssetTableQueryInformationAsync(context);
                var tenantContext = scope.ServiceProvider.GetService<ITenantContext>();
                var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(HttpClientNames.ASSET_TABLE, tenantContext);
                var payload = queryCriteria != null ? JsonConvert.SerializeObject(queryCriteria) : "{}";
                var request = new HttpRequestMessage(HttpMethod.Post, $"tbl/tables/{tableId}/query");
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                var responseMessage = await httpClient.SendAsync(request);
                if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new EntityNotFoundException();
                }
                responseMessage.EnsureSuccessStatusCode();
                var body = await responseMessage.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<IEnumerable<IDictionary<string, object>>>(body);
                return new BlockQueryResult(result, BindingDataTypeIdConstants.TYPE_ASSET_TABLE, null);
            }
        }
    }
}