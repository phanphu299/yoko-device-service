using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Constant;
using AHI.Infrastructure.UserContext.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.EntityLock.Command;
using Device.Application.Service.Abstraction;
using System.Collections.Generic;
using AHI.Infrastructure.Exception;
using System.Net.Http;
using AHI.Infrastructure.SharedKernel.Extension;
using Newtonsoft.Json;
using Device.Application.EntityLock.Command.Model;
using AHI.Infrastructure.MultiTenancy.Extension;

namespace Device.Application.Service
{
    public class EntityLockService : IEntityLockService
    {
        private readonly IUserContext _userContext;
        private readonly ITenantContext _tenantContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public EntityLockService(
            IUserContext userContext,
            ITenantContext tenantContext,
            IHttpClientFactory httpClientFactory)
        {
            _userContext = userContext;
            _tenantContext = tenantContext;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<BaseResponse> AcceptEntityUnlockRequestAsync(AcceptEntityUnlockRequestCommand command, CancellationToken token)
        {
            var entityService = _httpClientFactory.CreateClient(HttpClientNames.ENTITY_SERVICE, _tenantContext);
            var responseMessage = await entityService.PostAsync($"ent/locks/{command.TargetId}/lock/request/release/accept", new StringContent(JsonConvert.SerializeObject(new
            {
                RequestLockUpn = _userContext.Upn,
                Timeout = 0
            }), System.Text.Encoding.UTF8, "application/json"));
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = await responseMessage.Content.ReadAsByteArrayAsync();
                var accepted = responseData.Deserialize<BaseResponse>();
                return accepted;
            }
            return BaseResponse.Failed;
        }

        public async Task<bool> ValidateEntityLockedByOtherAsync(ValidateLockEntityCommand command, CancellationToken token)
        {
            var lockEntity = await GetEntityLockedAsync(command.TargetId);
            if (lockEntity != null && lockEntity.CurrentUserUpn != command.HolderUpn)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> ValidateEntitiesLockedByOtherAsync(ValidateLockEntitiesCommand command, CancellationToken token)
        {
            var entityIds = command.TargetIds.ToArray();
            var entities = await GetLockEntitesAsync(entityIds, command.HolderUpn);
            return entities.Any();
        }
        // public async Task<IEnumerable<Guid>> GetLockedEntityIdsFromListAsync(GetLockedEntityIdsFromListCommand command, CancellationToken token)
        // {
        //     var entityIds = command.TargetIds.ToArray();
        //     return await GetLockEntitesAsync(entityIds, command.HolderUpn);
        // }
        private async Task<IEnumerable<Guid>> GetLockEntitesAsync(IEnumerable<Guid> entityIds, string upn)
        {
            var entityService = _httpClientFactory.CreateClient(HttpClientNames.ENTITY_SERVICE, _tenantContext);
            var responseMessage = await entityService.PostAsync($"ent/locks/lock/entities", new StringContent(JsonConvert.SerializeObject(new
            {
                TargetIds = entityIds,
                HolderUpn = upn
            }), System.Text.Encoding.UTF8, "application/json"));
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = await responseMessage.Content.ReadAsByteArrayAsync();
                var lockedEntityIds = responseData.Deserialize<IEnumerable<Guid>>();
                return lockedEntityIds;
            }
            else
            {
                throw new SystemCallServiceException();
            }
        }
        public async Task<LockEntityResponse> GetEntityLockedAsync(Guid entityId)
        {
            var entityService = _httpClientFactory.CreateClient(HttpClientNames.ENTITY_SERVICE, _tenantContext);
            var responseMessage = await entityService.GetAsync($"ent/locks/{entityId}/lock");
            if (responseMessage.IsSuccessStatusCode)
            {
                var body = await responseMessage.Content.ReadAsByteArrayAsync();
                var entityLock = body.Deserialize<LockEntityResponse>();
                return entityLock;
            }
            else
            {
                throw new SystemCallServiceException();
            }

        }
        // public async Task<IEnumerable<Domain.Entity.EntityLock>> GetLockedEntitiesAsync(IEnumerable<Guid> entityIds, string upn)
        // {
        //     var entityService = _httpClientFactory.CreateClient(HttpClientNames.ENTITY_SERVICE, _tenantContext);
        //     var responseMessage = await entityService.PostAsync($"ent/locks/entities/all", new StringContent(JsonConvert.SerializeObject(new
        //     {
        //         TargetIds = entityIds,
        //         HolderUpn = upn
        //     }), System.Text.Encoding.UTF8, "application/json"));
        //     if (responseMessage.IsSuccessStatusCode)
        //     {
        //         var responseData = await responseMessage.Content.ReadAsByteArrayAsync();
        //         var lockedEntityIds = responseData.Deserialize<IEnumerable<Domain.Entity.EntityLock>>();
        //         return lockedEntityIds;
        //     }
        //     else
        //     {
        //         throw new SystemCallServiceException();
        //     }
        // }
    }
}
