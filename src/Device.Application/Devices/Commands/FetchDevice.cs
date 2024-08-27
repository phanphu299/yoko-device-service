using Device.Application.Device.Command.Model;
using MediatR;

namespace Device.Application.Device.Command
{
    public class FetchDevice : IRequest<GetDeviceDto>
    {
        public string Id { get; set; }

        public FetchDevice(string id)
        {
            Id = id;
        }
    }
}