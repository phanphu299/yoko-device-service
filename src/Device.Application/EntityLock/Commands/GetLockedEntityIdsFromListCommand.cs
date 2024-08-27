using System;
using System.Collections.Generic;
using MediatR;

namespace Device.Application.EntityLock.Command
{
    public class GetLockedEntityIdsFromListCommand : BaseEntityLock, IRequest<IEnumerable<Guid>>
    {
        public IEnumerable<Guid> TargetIds { get; private set; }
        public GetLockedEntityIdsFromListCommand()
            : this(new List<Guid>())
        { }
        public GetLockedEntityIdsFromListCommand(IEnumerable<Guid> targetIds)
        {
            TargetIds = targetIds;
        }
    }
}
