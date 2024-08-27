using MediatR;

namespace Device.Application.EntityLock.Command
{
    public class ValidateLockEntityCommand : BaseEntityLock, IRequest<bool>
    {

    }
}
