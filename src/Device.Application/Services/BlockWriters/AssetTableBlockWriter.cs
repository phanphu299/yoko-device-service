using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Device.Application.BlockFunction.Model;
using Device.Application.Constant;
using Device.Application.Enum;
using Device.Application.Extension;
using Device.Application.Service.Abstraction;
using Device.ApplicationExtension.Extension;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Device.Application.Service
{
    public class AssetTableBlockWriter : BaseBlockWriter
    {
        public AssetTableBlockWriter(IBlockWriter next,
                                    IServiceProvider serviceProvider) : base(next, serviceProvider)
        {
        }

        protected override BlockOperator CurrentBlockOperator => BlockOperator.WriteAssetTableData;

        protected override async Task<Guid> ExecuteOperationAsync(IBlockContext context, params BlockDataRequest[] values)
        {
            using (var scope = _serviceProvider.CreateNewScope())
            {
                var (assetId, tableId, data) = await GetAssetTableUpsertInformationAsync(context);
                var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();
                var tenantContext = scope.ServiceProvider.GetService<ITenantContext>();
                var logger = scope.ServiceProvider.GetService<ILoggerAdapter<AssetTableBlockWriter>>();
                var httpClient = httpClientFactory.CreateClient(HttpClientNames.ASSET_TABLE, tenantContext);
                var payload = data.JsonSerializeKeepDictionaryCase(); // With Column table, if Column is "Value" => with normal JsonSerialize will reformat to "value" - which caused error in table-service.

                var request = new HttpRequestMessage(HttpMethod.Post, $"tbl/tables/{tableId}/upsert?callSource=System");
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                var responseMessage = await httpClient.SendAsync(request);

                if (responseMessage.IsSuccessStatusCode)
                {
                    return assetId;
                }
                switch (responseMessage.StatusCode)
                {
                    case System.Net.HttpStatusCode.NotFound:
                        throw new EntityNotFoundException();
                    default:
                        var error = await responseMessage.Content.ReadAsStringAsync();
                        var errorDetail = new
                        {
                            Payload = payload,
                            TableId = tableId,
                            AssetId = assetId,
                            ErrorResponse = error
                        };
                        logger.LogError($"Writing value for table failed: {JsonConvert.SerializeObject(errorDetail)}");
                        throw new GenericProcessFailedException(MessageConstants.BLOCK_EXECUTION_ERROR_CALL_SERVICE);
                }
            }
        }
    }
}
