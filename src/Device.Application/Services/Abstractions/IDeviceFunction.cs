using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.Asset.Command.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IDeviceFunction
    {
        Task CalculateRuntimeAsync(IEnumerable<AssetAttributeDto> assets);
        Task CalculateRuntimeBasedOnTriggerAsync(IEnumerable<AssetAttributeDto> assets);
    }
}
