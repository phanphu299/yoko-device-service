using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.UserContext.Abstraction;
using Device.Application.Service.Abstraction;
using Newtonsoft.Json;

namespace Device.Application.Service
{
    public class TokenService : ITokenService
    {
        private readonly ICache _cache;
        private readonly ITenantContext _tenantContext;
        private readonly IUserContext _userContext;
        private readonly IUserService _userService;

        public TokenService(ICache cache, ITenantContext tenantContext, IUserContext userContext, IUserService userService)
        {
            _cache = cache;
            _tenantContext = tenantContext;
            _userContext = userContext;
            _userService = userService;
        }

        public async Task<bool> CheckTokenAsync(string token, string prefix = null)
        {
            if (!string.IsNullOrEmpty(prefix) && !token.StartsWith(prefix))
            {
                await _cache.DeleteAsync(token);
                return false;
            }
            var result = await _cache.GetStringAsync(token);
            if (!string.IsNullOrEmpty(result))
            {
                var tenantInfo = JsonConvert.DeserializeObject<TenantInfo>(result);
                _tenantContext.SetTenantId(tenantInfo.TenantId);
                _tenantContext.SetSubscriptionId(tenantInfo.SubscriptionId);
                _tenantContext.SetProjectId(tenantInfo.ProjectId);
                if (!string.IsNullOrEmpty(tenantInfo.Upn))
                {
                    var userInfo = await _userService.GetUserInfoAsync(tenantInfo.Upn, CancellationToken.None);
                    if (userInfo != null)
                    {
                        _userContext.SetId(userInfo.Id);
                        _userContext.SetUpn(userInfo.Upn);
                        _userContext.SetName(userInfo.FirstName, userInfo.MiddleName, userInfo.LastName);
                        _userContext.SetRightShorts(userInfo.RightShorts);
                        _userContext.SetObjectRightShorts(userInfo.ObjectRightShorts);
                    }
                }
                await _cache.DeleteAsync(token);
                return true;
            }
            return false;
        }
    }

    internal class TenantInfo
    {
        public string TenantId { get; set; }
        public string SubscriptionId { get; set; }
        public string ProjectId { get; set; }
        public string Upn { get; set; }
    }

    public class UserInfo
    {
        public Guid Id { get; set; }
        public string Upn { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public IEnumerable<string> RightShorts { get; set; }
        public IEnumerable<string> ObjectRightShorts { get; set; }
        public UserInfo()
        {
            RightShorts = new List<string>();
            ObjectRightShorts = new List<string>();
        }
    }
}
