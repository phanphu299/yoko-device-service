using MediatR;
using AHI.Infrastructure.SharedKernel.Model;
namespace Device.Application.EntityLock.Command
{
    public class RejectEntityUnlockRequestCommand : BaseEntityLock, IRequest<BaseResponse>
    {
        public string RejectedUserUpn { get; set; }
    }
}
