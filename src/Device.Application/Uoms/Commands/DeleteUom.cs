using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Uom.Command
{
    public class DeleteUom : IRequest<BaseResponse>
    {
        public int[] Ids { get; set; }
        public DeleteUom(int[] ids)
        {
            Ids = ids;
        }
    }
}
