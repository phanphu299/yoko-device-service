using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Application.Asset.Command.Model;

namespace Device.Application.Service
{
    public class DeviceFunction : IDeviceFunction
    {

        private readonly IHttpClientFactory _httpClientFactory;

        private readonly ITenantContext _tenantContext;
        private readonly ILogger<DeviceFunction> _logger;
        private const string RUNTIME_BASE = "fnc/dev/devices/calculate";
        private const string TRIGGER_BASE = "fnc/dev/devices/calculate/trigger";
        public DeviceFunction(IHttpClientFactory clientFactory, ITenantContext tenantContext, ILogger<DeviceFunction> logger)
        {
            _httpClientFactory = clientFactory;
            _tenantContext = tenantContext;
            _logger = logger;
        }
        public Task CalculateRuntimeAsync(IEnumerable<AssetAttributeDto> assets)
        {
            return CalculateAsync(RUNTIME_BASE, assets);
        }

        public Task CalculateRuntimeBasedOnTriggerAsync(IEnumerable<AssetAttributeDto> assets)
        {
            return CalculateAsync(TRIGGER_BASE, assets);
        }
        public async Task CalculateAsync(string endpoint, IEnumerable<AssetAttributeDto> assetAttributes)
        {
            if (assetAttributes == null || !assetAttributes.Any())
            {
                return;
            }
            var client = _httpClientFactory.CreateClient(HttpClientNames.DEVICE_FUNCTION);
            var message = JsonConvert.SerializeObject(new
            {
                TenantId = _tenantContext.TenantId,
                SubscriptionId = _tenantContext.SubscriptionId,
                ProjectId = _tenantContext.ProjectId,
                Assets = assetAttributes
            });
            var requestBody = new StringContent(message, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, requestBody);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Calling into device function is not successful, {content}");
            }
        }
    }
}
