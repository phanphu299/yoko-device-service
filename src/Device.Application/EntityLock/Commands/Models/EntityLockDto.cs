using System;

namespace Device.Application.Asset.Command.Model
{
    public class EntityLockDto
    {
        public Guid TargetId { get; set; }
        public string CurrentUserUpn { get; set; }
    }
}
