using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Constant;
using Device.Application.EventForwarding.Command;
using Device.Application.EventForwarding.Command.Model;
using Device.Application.Service.Abstraction;
using Device.ApplicationExtension.Extension;
using Newtonsoft.Json;

namespace Device.Application.Service
{
    public class EventForwardingService : IEventForwardingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;

        public EventForwardingService(IHttpClientFactory httpClientFactory, ITenantContext tenantContext)
        {
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
        }

        public async Task<IEnumerable<EventForwardingDto>> GetEventForwardingUsingAssetAsync(GetEventForwardingUsingAsset command, CancellationToken cancellationToken)
        {
            var eventService = _httpClientFactory.CreateClient(HttpClientNames.EVENT_SERVICE, _tenantContext);
            var searchContent = new StringContent(JsonConvert.SerializeObject(new
            {
                PageSize = int.MaxValue,
                Filter = JsonConvert.SerializeObject(new
                {
                    and = new object[]
                    {
                        new
                        {
                            queryKey = "isVisible",
                            queryType = QueryType.BOOLEAN,
                            operation = QueryOperation.EQUAL,
                            queryValue = "true"
                        },
                        new
                        {
                            or = command.AssetIds.Select(x => new
                                {
                                    queryKey = $"Assets.Any(x=>x.AssetId.ToString() == {x.ToStringQuote()})",
                                    queryType = QueryType.BOOLEAN,
                                    operation = QueryOperation.EQUAL,
                                    queryValue = "true"
                                }
                            )
                        }
                    }
                }),
                Fields = new[] { "id", "name" },
            }), System.Text.Encoding.UTF8, "application/json");

            var eventResponseSearchMessage = await eventService.PostAsync("evn/eventforwardings/search", searchContent);
            eventResponseSearchMessage.EnsureSuccessStatusCode();

            var body = await eventResponseSearchMessage.Content.ReadAsByteArrayAsync();
            var response = body.Deserialize<BaseSearchResponse<EventForwardingDto>>();

            return response.Data;
        }
    }
}
