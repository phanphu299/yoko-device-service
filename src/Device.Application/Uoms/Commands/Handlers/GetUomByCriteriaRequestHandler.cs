using System.Threading;
using System.Threading.Tasks;
using Device.Application.Uom.Command;
using Device.Application.Uom.Command.Model;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class GetUomByCriteriaRequestHandler : IRequestHandler<GetUomByCriteria, BaseSearchResponse<GetUomDto>>
    {
        private readonly IUomService _service;
        public GetUomByCriteriaRequestHandler(IUomService service)
        {
            _service = service;
        }

        public async Task<BaseSearchResponse<GetUomDto>> Handle(GetUomByCriteria request, CancellationToken cancellationToken)
        {
            return await _service.SearchAsync(request);
        }
    }
}
