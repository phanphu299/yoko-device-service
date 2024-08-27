using System;
using System.Collections.Generic;
using MediatR;

namespace Device.Application.EntityLock.Command
{
    public class ValidateLockEntitiesCommand : BaseEntityLock, IRequest<bool>
    {
        public IEnumerable<Guid> TargetIds { get; private set; }
        public ValidateLockEntitiesCommand()
            : this(new List<Guid>())
        { }

        public ValidateLockEntitiesCommand(IEnumerable<Guid> targetIds)
        {
            TargetIds = targetIds;
        }
    }
}
