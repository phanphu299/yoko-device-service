using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;
using Device.Application.TemplateDetail.Command;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.Template.Command.Handler
{
    public class ValidattionTemplateDetailsRequestHandler : IRequestHandler<ValidationTemplateDetails, BaseResponse>
    {
        private readonly IDeviceTemplateService _service;
        public ValidattionTemplateDetailsRequestHandler(IDeviceTemplateService service)
        {
            _service = service;
        }

        public async Task<BaseResponse> Handle(ValidationTemplateDetails request, CancellationToken cancellationToken)
        {
            var result = await _service.ValidationTemplateDetailsAsync(request.Id, request.Keys);
            if (result)
                return BaseResponse.Success;
            else
                return BaseResponse.Failed;
        }
    }
}
