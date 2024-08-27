using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.EventForwarding.Command;
using Device.Application.EventForwarding.Command.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IEventForwardingService
    {
        Task<IEnumerable<EventForwardingDto>> GetEventForwardingUsingAssetAsync(GetEventForwardingUsingAsset command, CancellationToken cancellationToken);
    }
}
