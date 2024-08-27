using MediatR;
using Device.Application.Device.Command.Model;
using System.Collections.Generic;

namespace Device.Application.Device.Command
{
    public class GetMetricsByDeviceId : IRequest<IEnumerable<GetMetricsByDeviceIdDto>>
    {
        public string DeviceId { get; set; }
        public bool IsIncludeDisabledMetric { get; set; }

        public GetMetricsByDeviceId(string deviceId, bool isIncludeDisabledMetric)
        {
            DeviceId = deviceId;
            IsIncludeDisabledMetric = isIncludeDisabledMetric;
        }
    }
}