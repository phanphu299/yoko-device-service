using Device.Application.Uom.Command.Model;
using MediatR;

namespace Device.Application.Uom.Command
{
    public class CalculationRefUom : IRequest<CalculationRefUomDto>
    {
        public int RefId { set; get; }
        public double Factor { get; set; }
        public double Offset { get; set; }
    }
}
