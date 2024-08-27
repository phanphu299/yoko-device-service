using System.Threading;
using System.Threading.Tasks;
using Device.Application.TemplateKeyType.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.TemplateKeyType.Command.Handler
{
    public class GetTemplateKeyTypeByCriteriaRequestHandler : IRequestHandler<GetTemplateKeyTypeByCriteria, BaseSearchResponse<GetTemplateKeyTypeDto>>
    {
        private readonly ITemplateKeyTypesService _service;
        public GetTemplateKeyTypeByCriteriaRequestHandler(ITemplateKeyTypesService service)
        {
            _service = service;
        }

        public async Task<BaseSearchResponse<GetTemplateKeyTypeDto>> Handle(GetTemplateKeyTypeByCriteria request, CancellationToken cancellationToken)
        {
            return await _service.SearchAsync(request);
        }
    }
}
