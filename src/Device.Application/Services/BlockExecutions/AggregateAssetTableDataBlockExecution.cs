using System;
using System.Threading.Tasks;
using Device.Application.Enum;
using Device.Application.BlockFunction.Model;
using Device.Application.Service.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Device.Application.Constant;
using System.Net.Http;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using Newtonsoft.Json;
using System.Text;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.Application.Extension;

namespace Device.Application.Service
{
    public class AggregateAssetTableDataBlockExecution : BaseBlockExecution
    {
        public AggregateAssetTableDataBlockExecution(IBlockExecution next,
                                                    IServiceProvider serviceProvider) : base(next, serviceProvider)
        {
        }

        protected override BlockOperator CurrentBlockOperator => BlockOperator.AggregateAssetTableData;

        protected override async Task<BlockQueryResult> ExecuteOperationAsync(IBlockContext context)
        {
            using (var scope = _serviceProvider.CreateNewScope())
            {
                var (assetId, tableId, columnName, aggregationCriteria) = await GetAssetTableAggregationInformationAsync(context);
                var tenantContext = scope.ServiceProvider.GetService<ITenantContext>();
                var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(HttpClientNames.ASSET_TABLE, tenantContext);
                var payload = JsonConvert.SerializeObject(new { ColumnName = columnName, AggregationCriteria = aggregationCriteria });
                var request = new HttpRequestMessage(HttpMethod.Post, $"tbl/tables/{tableId}/aggregate");
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                var responseMessage = await httpClient.SendAsync(request);
                if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new EntityNotFoundException();
                }
                responseMessage.EnsureSuccessStatusCode();
                var body = await responseMessage.Content.ReadAsByteArrayAsync();
                var result = body.Deserialize<object>();

                return new BlockQueryResult(result, BindingDataTypeIdConstants.TYPE_ASSET_TABLE, null);
            }
        }
    }
}