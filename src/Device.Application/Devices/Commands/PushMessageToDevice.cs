using MediatR;
using AHI.Infrastructure.SharedKernel.Model;
using System.Collections.Generic;

namespace Device.Application.Device.Command.Model
{
    public class PushMessageToDevice : IRequest<BaseResponse>
    {
        public string Id { get; set; }
        public IEnumerable<CloudToDeviceMessage> Metrics { get; set; }
        public PushMessageToDevice(string id, IEnumerable<CloudToDeviceMessage> metrics)
        {
            Id = id;
            Metrics = metrics;
        }
    }
    public class CloudToDeviceMessage
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
    }
}
