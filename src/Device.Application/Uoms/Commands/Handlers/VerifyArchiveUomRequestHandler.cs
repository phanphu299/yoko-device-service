using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using Device.Application.Uom.Command;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class VerifyArchiveUomRequestHandler : IRequestHandler<VerifyUom, BaseResponse>
    {
        private readonly IUomService _service;

        public VerifyArchiveUomRequestHandler(IUomService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(VerifyUom request, CancellationToken cancellationToken)
        {
            return _service.VerifyArchiveAsync(request, cancellationToken);
        }
    }
}
