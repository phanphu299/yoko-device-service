using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using Newtonsoft.Json;

namespace Function.Services;
public class FunctionBlockExecutionService : IFunctionBlockExecutionService
{
    private readonly ITenantContext _tenantContext;
    private readonly IHttpClientFactory _httpClientFactory;

    public FunctionBlockExecutionService(ITenantContext tenantContext, IHttpClientFactory httpClientFactory)
    {
        _tenantContext = tenantContext;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IEnumerable<TriggerAttributeFunctionBlockExecution>> FindFunctionBlockExecutionByAssetIds(IEnumerable<Guid> assetIds)
    {
        var client = _httpClientFactory.CreateClient(ClientNameConstant.FUNCTION_BLOCK, _tenantContext);
        var payload = new
        {
            AssetIds = assetIds
        };
        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, mediaType: "application/json");
        var responseMessage = await client.PostAsync($"fbk/blockexecutions/trigger/attribute", content);
        responseMessage.EnsureSuccessStatusCode();
        var messages = await responseMessage.Content.ReadAsStringAsync();
        var response = JsonConvert.DeserializeObject<IEnumerable<TriggerAttributeFunctionBlockExecution>>(messages);
        return response;
    }
}