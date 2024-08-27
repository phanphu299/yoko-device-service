using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using Device.Application.Asset.Command;
using Device.Application.BlockFunction.Model;
using Device.Application.Constant;
using Device.Application.Enum;
using Device.Application.Extension;
using Device.Application.Service.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Device.Application.Service
{
    public class AssetTableBlockDelete : BaseBlockWriter
    {
        public AssetTableBlockDelete(IBlockWriter next,
                                    IServiceProvider serviceProvider) : base(next, serviceProvider)
        {
        }

        protected override BlockOperator CurrentBlockOperator => BlockOperator.DeleteAssetTableData;

        protected override async Task<Guid> ExecuteOperationAsync(IBlockContext context, params BlockDataRequest[] values)
        {
            using (var scope = _serviceProvider.CreateNewScope())
            {
                var (assetId, tableId, ids) = await GetAssetTableDeleteInformationAsync(context);
                var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();
                var tenantContext = scope.ServiceProvider.GetService<ITenantContext>();
                var httpClient = httpClientFactory.CreateClient(HttpClientNames.ASSET_TABLE, tenantContext);
                var payload = JsonConvert.SerializeObject(new DeleteTableData(tableId, ids));
                var request = new HttpRequestMessage(HttpMethod.Delete, $"tbl/tables/{tableId}/delete");
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                var responseMessage = await httpClient.SendAsync(request);
                if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new EntityNotFoundException();
                }
                responseMessage.EnsureSuccessStatusCode();
                return assetId;
            }
        }
    }
}