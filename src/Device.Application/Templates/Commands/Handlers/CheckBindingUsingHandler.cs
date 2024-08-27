using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Template.Command.Handler
{
    public class CheckBindingUsingHandler : IRequestHandler<CheckBindingUsing, BaseResponse>
    {
        private readonly IDeviceTemplateService _service;
        public CheckBindingUsingHandler(IDeviceTemplateService service)
        {
            _service = service;
        }

        public async Task<BaseResponse> Handle(CheckBindingUsing request, CancellationToken cancellationToken)
        {
            var result = await _service.CheckBindingUsingAsync(request, cancellationToken);
            return result ? BaseResponse.Success : BaseResponse.Failed;
        }
    }
}
