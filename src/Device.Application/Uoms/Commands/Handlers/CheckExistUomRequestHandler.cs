using MediatR;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;

namespace Device.Application.Uom.Command.Handler
{
    public class CheckExistUomRequestHandler : IRequestHandler<CheckExistUom, BaseResponse>
    {
        private readonly IUomService _service;

        public CheckExistUomRequestHandler(IUomService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(CheckExistUom request, CancellationToken cancellationToken)
        {
            return _service.CheckExistUomsAsync(request, cancellationToken);
        }
    }
}