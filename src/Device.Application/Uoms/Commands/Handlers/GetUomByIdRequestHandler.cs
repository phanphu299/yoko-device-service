using System.Threading;
using System.Threading.Tasks;
using Device.Application.Uom.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Uom.Command.Handler
{
    public class GetUomByIdRequestHandler : IRequestHandler<GetUomById, GetUomDto>
    {
        private readonly IUomService _service;
        public GetUomByIdRequestHandler(IUomService service)
        {
            _service = service;
        }

        public Task<GetUomDto> Handle(GetUomById request, CancellationToken cancellationToken)
        {
            // persist to database and implement extra logic
            return _service.FindUomByIdAsync(request, cancellationToken);
        }
    }
}
