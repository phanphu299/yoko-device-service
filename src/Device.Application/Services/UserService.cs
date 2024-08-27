using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using System.Net.Http;
using Device.Application.Models;
using Newtonsoft.Json;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Application.Constant;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.SharedKernel.Extension;

namespace Device.Application.Service
{
    public class UserService : IUserService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;

        public UserService(IHttpClientFactory httpClientFactory, ITenantContext tenantContext)
        {
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
        }

        public async Task<UserInfoDto> GetUserInfoAsync(string upn, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(HttpClientNames.USER_FUNCTION);
            var payload = await client.PostAsync(
                $"fnc/usr/users/info",
                new StringContent(
                    JsonConvert.SerializeObject(new
                    {
                        Upn = upn,
                        TenantId = _tenantContext.TenantId,
                        SubscriptionId = _tenantContext.SubscriptionId
                    }),
                    System.Text.Encoding.UTF8, "application/json"
                )
            );
            if (payload.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new EntityNotFoundException();
            }
            payload.EnsureSuccessStatusCode();
            var stream = await payload.Content.ReadAsByteArrayAsync();
            var userInfo = stream.Deserialize<UserInfoDto>();
            return userInfo;
        }
    }
}
