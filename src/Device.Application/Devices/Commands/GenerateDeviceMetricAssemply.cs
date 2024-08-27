using Device.Application.Device.Command.Model;
using MediatR;

namespace Device.Application.Device.Command
{
    public class GenerateDeviceMetricAssemply : IRequest<MetricAssemblyDto>
    {
        public string Id { get; set; }
        public GenerateDeviceMetricAssemply(string deviceId)
        {
            Id = deviceId;
        }
    }
}
