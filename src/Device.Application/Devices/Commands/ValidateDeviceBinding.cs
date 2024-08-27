using Device.Application.Device.Command.Model;
using MediatR;
using System;
using System.Collections.Generic;


namespace Device.Application.Device.Command
{
    public class ValidateDeviceBindings : IRequest<IEnumerable<ValidateDeviceBindingDto>>
    {
        public IEnumerable<ValidateDeviceBinding> ValidateBindings { get; set; }
    }
    public class ValidateDeviceBinding : IRequest<IEnumerable<ValidateDeviceBindingDto>>
    {
        public Guid AssetId { get; set; }
        public Guid DeviceTemplateId { get; set; }
        public Guid CommandAttributeId { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
    }
}