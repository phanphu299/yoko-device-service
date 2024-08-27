using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Template.Command.Handler
{
    public class CheckMetricUsingHandler : IRequestHandler<CheckMetricUsing, BaseResponse>
    {
        private readonly IDeviceTemplateService _service;
        public CheckMetricUsingHandler(IDeviceTemplateService service)
        {
            _service = service;
        }

        public async Task<BaseResponse> Handle(CheckMetricUsing request, CancellationToken cancellationToken)
        {
            var result = await _service.CheckMetricUsingAsync(request, cancellationToken);
            return result ? BaseResponse.Success : BaseResponse.Failed;
        }
    }
}
