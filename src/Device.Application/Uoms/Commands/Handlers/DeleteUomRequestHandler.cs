using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Uom.Command.Handler
{
    public class DeleteUomRequestHandler : IRequestHandler<DeleteUom, BaseResponse>
    {
        private readonly IUomService _service;
        public DeleteUomRequestHandler(IUomService service)
        {
            _service = service;
        }

        public async Task<BaseResponse> Handle(DeleteUom request, CancellationToken cancellationToken)
        {
            return await _service.RemoveUomAsync(request, cancellationToken);
        }
    }
}
