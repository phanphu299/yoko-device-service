using MediatR;

namespace Device.Application.Device.Command
{
    public class AddDeviceToCache
    {
        public string DeviceId { get; set; }
        public AddDeviceToCache() { }
        public AddDeviceToCache(string deviceId) { DeviceId = deviceId; }
    }
}