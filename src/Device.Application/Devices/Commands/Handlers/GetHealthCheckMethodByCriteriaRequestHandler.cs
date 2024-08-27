using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class GetHealthCheckMethodByCriteriaRequestHandler : IRequestHandler<GetHealthCheckMethodByCriteria, BaseSearchResponse<GetHealthCheckMethodDto>>
    {
        private readonly IHealthCheckMethodService _service;
        public GetHealthCheckMethodByCriteriaRequestHandler(IHealthCheckMethodService service)
        {
            _service = service;
        }

        public Task<BaseSearchResponse<GetHealthCheckMethodDto>> Handle(GetHealthCheckMethodByCriteria request, CancellationToken cancellationToken)
        {
            return _service.SearchAsync(request);
        }
    }
}
