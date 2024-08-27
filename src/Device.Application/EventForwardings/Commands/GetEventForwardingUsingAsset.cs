using System;
using System.Collections.Generic;
using Device.Application.EventForwarding.Command.Model;
using MediatR;

namespace Device.Application.EventForwarding.Command
{
    public class GetEventForwardingUsingAsset : IRequest<IEnumerable<EventForwardingDto>>
    {
        public IEnumerable<Guid> AssetIds { get; set; }

        public GetEventForwardingUsingAsset(IEnumerable<Guid> assetIds)
        {
            AssetIds = assetIds;
        }
    }
}
