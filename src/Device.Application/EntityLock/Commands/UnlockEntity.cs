using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.EntityLock.Command
{
    public class UnlockEntity : BaseEntityLock, IRequest<EntityLockDto>
    {
        public string RequestUnlockUpn { get; set; }
        public int Timeout { set; get; }
    }
}
