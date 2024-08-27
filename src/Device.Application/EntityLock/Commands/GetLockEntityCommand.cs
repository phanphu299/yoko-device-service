using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.EntityLock.Command
{
    public class GetLockEntityCommand : BaseEntityLock, IRequest<EntityLockDto>
    {
    }
}
