using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using MediatR;
using Device.Application.Service.Abstraction;
using System.Collections.Generic;

namespace Device.Application.Device.Command.Handler
{
    public class SearchSharingBrokerHandler : IRequestHandler<SearchSharingBroker, IEnumerable<SharingBrokerDto>>
    {
        private readonly IDeviceService _service;

        public SearchSharingBrokerHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<IEnumerable<SharingBrokerDto>> Handle(SearchSharingBroker request, CancellationToken cancellationToken)
        {
            return _service.SearchSharingBrokerAsync(request, cancellationToken);
        }
    }
}