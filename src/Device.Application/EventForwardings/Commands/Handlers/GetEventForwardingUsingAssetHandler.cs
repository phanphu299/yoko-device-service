using Device.Application.Service.Abstraction;
using MediatR;
using System.Threading.Tasks;
using System.Threading;
using Device.Application.EventForwarding.Command.Model;
using System.Collections.Generic;

namespace Device.Application.EventForwarding.Command.Handler
{
    public class GetEventForwardingUsingAssetHandler : IRequestHandler<GetEventForwardingUsingAsset, IEnumerable<EventForwardingDto>>
    {
        private readonly IEventForwardingService _eventForwardingService;

        public GetEventForwardingUsingAssetHandler(IEventForwardingService eventForwardingService)
        {
            _eventForwardingService = eventForwardingService;
        }

        public Task<IEnumerable<EventForwardingDto>> Handle(GetEventForwardingUsingAsset request, CancellationToken cancellationToken)
        {
            return _eventForwardingService.GetEventForwardingUsingAssetAsync(request, cancellationToken);
        }
    }
}
