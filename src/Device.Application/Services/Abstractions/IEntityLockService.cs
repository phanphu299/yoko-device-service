using System;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.EntityLock.Command;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.EntityLock.Command.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IEntityLockService
    {
        Task<bool> ValidateEntityLockedByOtherAsync(ValidateLockEntityCommand command, CancellationToken token);
        Task<bool> ValidateEntitiesLockedByOtherAsync(ValidateLockEntitiesCommand command, CancellationToken token);
        Task<BaseResponse> AcceptEntityUnlockRequestAsync(AcceptEntityUnlockRequestCommand command, CancellationToken token);
        Task<LockEntityResponse> GetEntityLockedAsync(Guid entityId);
    }
}
