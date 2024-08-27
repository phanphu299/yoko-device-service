using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using Device.Application.Uom.Command.Model;
using MediatR;

namespace Device.Application.Uom.Command.Handler
{
    public class FetchUomRequestHandler : IRequestHandler<FetchUom, GetUomDto>
    {
        private readonly IUomService _service;

        public FetchUomRequestHandler(IUomService service)
        {
            _service = service;
        }

        public Task<GetUomDto> Handle(FetchUom request, CancellationToken cancellationToken)
        {
            return _service.FetchAsync(request.Id);
        }
    }
}