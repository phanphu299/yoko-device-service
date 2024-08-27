using Device.Application.Device.Command;

namespace Device.Application.Model
{
    internal class UpsertDeviceException
    {
        public UpdateDevice Request { get; set; }
        public string Message { get; set; }
    }
}