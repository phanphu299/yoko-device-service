using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Extensions;
using Device.Consumer.KraftShared.Constant;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Model.SearchModel;
using Device.Consumer.KraftShared.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;

namespace Device.Consumer.KraftShared.Service
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