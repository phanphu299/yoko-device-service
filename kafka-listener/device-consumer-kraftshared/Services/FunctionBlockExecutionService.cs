using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using Device.Consumer.KraftShared.Constant;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Service.Abstraction;


namespace Device.Consumer.KraftShared.Services;
public class FunctionBlockExecutionService : IFunctionBlockExecutionService
{
    private readonly ITenantContext _tenantContext;
    private readonly IHttpClientFactory _httpClientFactory;

    public FunctionBlockExecutionService(ITenantContext tenantContext, IHttpClientFactory httpClientFactory)
    {
        _tenantContext = tenantContext;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IEnumerable<TriggerAttributeFunctionBlockExecution>> FindFunctionBlockExecutionByAssetIds(string tenantId, string subscriptionId, string projectId, 
        IEnumerable<Guid> assetIds)
    {
        _tenantContext.SetTenantId(tenantId);
        _tenantContext.SetSubscriptionId(subscriptionId);
        _tenantContext.SetProjectId(projectId);
        var client = _httpClientFactory.CreateClient(ClientNameConstant.FUNCTION_BLOCK, _tenantContext);
        var payload = new
        {
            AssetIds = assetIds
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, mediaType: "application/json");
        var responseMessage = await client.PostAsync($"fbk/blockexecutions/trigger/attribute", content);
        responseMessage.EnsureSuccessStatusCode();
        var messages = await responseMessage.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<IEnumerable<TriggerAttributeFunctionBlockExecution>>(messages);
        return response;
    }
}
