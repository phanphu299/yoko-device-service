using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Block.Command.Model;

namespace Device.Application.Block.Command.Handler
{
    public class SearchFunctionBlockByCriteriaHandler : IRequestHandler<GetFunctionBlockByCriteria, BaseSearchResponse<GetFunctionBlockSimpleDto>>
    {
        private readonly IFunctionBlockService _functionBlockService;
        public SearchFunctionBlockByCriteriaHandler(IFunctionBlockService functionBlockService)
        {
            _functionBlockService = functionBlockService;
        }
        public Task<BaseSearchResponse<GetFunctionBlockSimpleDto>> Handle(GetFunctionBlockByCriteria request, CancellationToken cancellationToken)
        {
            return _functionBlockService.SearchAsync(request);
        }
    }
}
