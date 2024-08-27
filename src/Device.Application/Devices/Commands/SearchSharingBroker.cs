using System.Collections.Generic;
using MediatR;

namespace Device.Application.Device.Command.Model
{
    public class SearchSharingBroker : IRequest<IEnumerable<SharingBrokerDto>>
    {
    }
}