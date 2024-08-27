using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Template.Command
{
    public class CheckBindingUsing : IRequest<BaseResponse>
    {
        public System.Guid Id { get; set; }
        public int BindingId { get; set; }

        public CheckBindingUsing(System.Guid id, int bindingId)
        {
            Id = id;
            BindingId = bindingId;
        }
    }
}
