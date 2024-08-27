using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using Device.Application.Uom.Command;
using Device.Application.Uom.Command.Model;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class ArchiveUomRequestHandler : IRequestHandler<ArchiveUom, ArchiveUomDataDto>
    {
        private readonly IUomService _service;

        public ArchiveUomRequestHandler(IUomService service)
        {
            _service = service;
        }

        public async Task<ArchiveUomDataDto> Handle(ArchiveUom request, CancellationToken cancellationToken)
        {
            var uoms = await _service.ArchiveAsync(request, cancellationToken);
            var result = new ArchiveUomDataDto
            {
                Uoms = uoms
            };
            return result;
        }
    }
}
