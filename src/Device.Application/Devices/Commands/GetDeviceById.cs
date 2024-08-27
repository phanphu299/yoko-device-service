using Device.Application.Device.Command.Model;
using MediatR;

namespace Device.Application.Device.Command
{
    public class GetDeviceById : IRequest<GetDeviceDto>
    {
        public string Id { get; set; }
        public GetDeviceById(string id)
        {
            Id = id;
        }
    }
}
