using System;

namespace Device.Application.EntityLock.Command
{
    public class BaseEntityLock
    {
        public Guid TargetId { get; set; }
        public string HolderUpn { get; set; }
        public string Type { get; set; }
    }
}