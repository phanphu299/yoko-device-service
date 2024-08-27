using System.Collections.Generic;
using Device.Application.Asset.Command.Model;
using MediatR;
namespace Device.Application.Asset.Command
{
    public class SendConfigurationToDeviceIotMutiple : IRequest<SendConfigurationResultMutipleDto>
    {
        public IEnumerable<SendConfigurationToDeviceIot> Data { get; set; }
    }
}