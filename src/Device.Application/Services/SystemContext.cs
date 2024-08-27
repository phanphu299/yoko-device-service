using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Device.Application.Constant;
using AHI.Infrastructure.MultiTenancy.Extension;
using Microsoft.Extensions.Caching.Memory;

namespace Device.Application.Service
{
    public class SystemContext : ISystemContext
    {
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<SystemContext> _logger;

        public SystemContext(IMemoryCache cache, IHttpClientFactory httpClientFactory, ITenantContext tenantContext, ILogger<SystemContext> logger)
        {
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<string> GetValueAsync(string key, string defaultValue, bool useCache = true)
        {
            var systemConfig = await GetFromServiceAsync(key, defaultValue);
            string cacheItem = systemConfig.Value;
            return cacheItem;
        }

        private async Task<SystemConfigDto> GetFromServiceAsync(string key, string defaultValue)
        {
            var httpClient = _httpClientFactory.CreateClient(HttpClientNames.CONFIGURATION, _tenantContext);
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