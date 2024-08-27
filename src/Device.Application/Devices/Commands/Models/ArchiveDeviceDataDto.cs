using System.Collections.Generic;
namespace Device.Application.Device.Command.Model
{
    public class ArchiveDeviceDataDto
    {
        public IEnumerable<ArchiveDeviceDto> Devices { get; set; }
        
        public ArchiveDeviceDataDto(IEnumerable<ArchiveDeviceDto> devices)
        {
            Devices = devices;
        }
    }
}
