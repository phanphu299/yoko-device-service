using Device.Application.Device.Command.Model;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;

namespace Device.Application.Device.Command
{
    public class PatchDevice : IRequest<UpdateDeviceDto>
    {
        public JsonPatchDocument JsonPatch { set; get; }

        public string Id { set; get; }
    }
}
