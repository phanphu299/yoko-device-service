using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Function.Constant;
using AHI.Infrastructure.MultiTenancy.Extension;

namespace AHI.Device.Function.Service
{
    public class SystemContext : ISystemContext
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger _logger;

        public SystemContext(IHttpClientFactory httpClientFactory, ITenantContext tenantContext, ILogger<SystemContext> logger)
        {
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
            _logger = logger;
        }
        public async Task<string> GetValueAsync(string key, string defaultValue, bool useCache = true)
        {
            var systemConfig = await GetFromServiceAsync(key, defaultValue);
            var cacheItem = systemConfig.Value;
            return cacheItem;

        }

        private async Task<SystemConfigDto> GetFromServiceAsync(string key, string defaultValue)
        {
            var httpClient = _httpClientFactory.CreateClient(ClientNameConstant.CONFIGURATION_SERVICE, _tenantContext);
            var response = await httpClient.GetAsync($"cnm/configs?key={key}");
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadAsStringAsync();
            try
            {
                var body = JsonConvert.DeserializeObject<BaseSearchResponse<SystemConfigDto>>(payload);
                return body.Data.ElementAt(0);
            }
            catch (System.Exception exc)
            {
                _logger.LogError(exc, exc.Message);
                return new SystemConfigDto(key, defaultValue);
            }
        }

    }
    class SystemConfigDto
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public SystemConfigDto(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
