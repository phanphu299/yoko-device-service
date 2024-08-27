using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Function.Extension;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Model;
using AHI.Device.Function.Model.SearchModel;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;

namespace AHI.Device.Function.Service
{
    public class IotBrokerService : IIotBrokerService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;

        public IotBrokerService(IHttpClientFactory httpClientFactory, ITenantContext tenantContext)
        {
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
        }

        public async Task<IEnumerable<BrokerDto>> SearchSharedBrokersAsync()
        {
            var projectClient = _httpClientFactory.CreateClient(ClientNameConstant.PROJECT_SERVICE, _tenantContext);
            try
            {
                var query = new FilteredSearchQuery();
                var response = await projectClient.SearchAsync<BrokerDto>($"prj/brokers/search", query);
                return response.Data;
            }
            catch (HttpRequestException)
            {
                return Array.Empty<BrokerDto>();
            }

        }
    }
}