using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using Device.Application.Uom.Command;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class RetrieveUomRequestHandler : IRequestHandler<RetrieveUom, BaseResponse>
    {
        private readonly IUomService _service;

        public RetrieveUomRequestHandler(IUomService service)
        {
            _service = service;
        }

        public async Task<BaseResponse> Handle(RetrieveUom request, CancellationToken cancellationToken)
        {
            return await _service.RetrieveAsync(request, cancellationToken);
        }
    }
}
