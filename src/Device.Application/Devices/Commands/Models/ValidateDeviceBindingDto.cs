using System;

namespace Device.Application.Device.Command.Model
{
    public class ValidateDeviceBindingDto
    {
        public Guid CommandAttributeId { get; set; }
        public bool IsValid { get; set; }
    }
}