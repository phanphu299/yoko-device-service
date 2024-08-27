using System.Threading;
using System.Threading.Tasks;
using Device.Application.Models;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Uom.Command.Handler
{
    public class ExportUomRequestHandler : IRequestHandler<ExportUom, ActivityResponse>
    {
        private readonly IUomService _service;

        public ExportUomRequestHandler(IUomService service)
        {
            _service = service;
        }

        public Task<ActivityResponse> Handle(ExportUom request, CancellationToken cancellationToken)
        {
            return _service.ExportAsync(request, cancellationToken);
        }
    }
}
