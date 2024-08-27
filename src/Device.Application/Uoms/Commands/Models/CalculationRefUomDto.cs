
namespace Device.Application.Uom.Command.Model
{
    public class CalculationRefUomDto
    {
        public GetUomDto RefUom { get; set; }
        public string Factor { get; set; }
        public string Offset { get; set; }
        public string CanonicalFactor { get; set; }
        public string CanonicalOffset { get; set; }
    }
}
