using System.Threading;
using System.Threading.Tasks;
using Device.Application.Uom.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Uom.Command.Handler
{
    public class CalculationRefUomRequestHandler : IRequestHandler<CalculationRefUom, CalculationRefUomDto>
    {
        private readonly IUomService _uomService;
        public CalculationRefUomRequestHandler(IUomService uomService)
        {
            _uomService = uomService;
        }

        public virtual async Task<CalculationRefUomDto> Handle(CalculationRefUom request, CancellationToken cancellationToken)
        {
            return await _uomService.CalculationRefUomAsync(request, cancellationToken);
        }

    }
}
