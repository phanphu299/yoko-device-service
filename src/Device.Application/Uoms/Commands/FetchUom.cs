using Device.Application.Uom.Command.Model;
using MediatR;

namespace Device.Application.Uom.Command
{
    public class FetchUom : IRequest<GetUomDto>
    {
        public int Id { get; set; }

        public FetchUom(int id)
        {
            Id = id;
        }
    }
}