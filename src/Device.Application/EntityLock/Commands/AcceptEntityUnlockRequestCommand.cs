using MediatR;
using AHI.Infrastructure.SharedKernel.Model;
namespace Device.Application.EntityLock.Command
{
    public class AcceptEntityUnlockRequestCommand : BaseEntityLock, IRequest<BaseResponse>
    {
    }
}
