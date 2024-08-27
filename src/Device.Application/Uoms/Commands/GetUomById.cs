using Device.Application.Uom.Command.Model;
using MediatR;

namespace Device.Application.Uom.Command
{
    public class GetUomById : IRequest<GetUomDto>
    {
        public int Id { get; set; }
        public GetUomById(int id)
        {
            Id = id;
        }
    }
}
