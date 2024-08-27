using System;

namespace Device.Application.EntityLock.Command.Model
{
    public class LockEntityResponse
    {
        public string CurrentUserUpn { get; set; }
        public Guid TargetId { get; set; }
    }
}
