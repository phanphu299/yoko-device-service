using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.BlockTemplate.Command;
using Device.Application.BlockTemplate.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockTemplate.Query.Handler
{
    public class GetBlockTemplateByCriteriaRequestHandler : IRequestHandler<GetFunctionBlockTemplateByCriteria, BaseSearchResponse<FunctionBlockTemplateSimpleDto>>
    {
        private readonly IFunctionBlockTemplateService _functionBlockTemplateService;
        public GetBlockTemplateByCriteriaRequestHandler(IFunctionBlockTemplateService functionBLockTemplateService)
        {
            _functionBlockTemplateService = functionBLockTemplateService;
        }

        public Task<BaseSearchResponse<FunctionBlockTemplateSimpleDto>> Handle(GetFunctionBlockTemplateByCriteria request, CancellationToken cancellationToken)
        {
            return _functionBlockTemplateService.SearchAsync(request);
        }
    }
}
