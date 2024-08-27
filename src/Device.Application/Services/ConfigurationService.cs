using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Constant;
using Device.Application.Models;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Newtonsoft.Json;
using AHI.Infrastructure.MultiTenancy.Extension;

namespace Device.Application.Service
{
    public class ConfigurationService : IConfigurationService
    {
        #region Properties

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;

        #endregion

        #region Constructor

        public ConfigurationService(IHttpClientFactory httpClientFactory, ITenantContext tenantContext)
        {
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <param name="code"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Lookup> FindLookupByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            var httpClient = _httpClientFactory.CreateClient(HttpClientNames.CONFIGURATION, _tenantContext);
            var endpoint = $"cnm/lookups/{code}";

            var httpResponseMessage = await httpClient.GetAsync(endpoint, cancellationToken);
            var szContent = await httpResponseMessage.Content.ReadAsStringAsync();

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
                    return null;

                httpResponseMessage.EnsureSuccessStatusCode();
            }

            if (string.IsNullOrEmpty(szContent))
                return null;

            return JsonConvert.DeserializeObject<Lookup>(szContent);
        }

        #endregion
    }
}
