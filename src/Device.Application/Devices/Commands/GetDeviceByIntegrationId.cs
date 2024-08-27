using System;
using System.Collections.Generic;
using MediatR;

namespace Device.Application.Device.Command
{
    public class GetDeviceByIntegrationId : IRequest<IEnumerable<string>>
    {
        public Guid IntegrationId { get; set; }
        public GetDeviceByIntegrationId(Guid id)
        {
            IntegrationId = id;
        }
    }
}
