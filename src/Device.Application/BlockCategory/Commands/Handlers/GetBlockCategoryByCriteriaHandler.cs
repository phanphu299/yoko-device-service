using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using AHI.Infrastructure.SharedKernel.Models;
using Device.Application.BlockFunctionCategory.Command;
using Device.Application.BlockFunctionCategory.Model;
using Device.Application.Service.Abstraction;
using Device.Application.SharedKernel;
using MediatR;
using Newtonsoft.Json;

namespace Device.Application.BlockCategory.Command.Handler
{
    public class GetBlockCategoryByCriteriaHandler : IRequestHandler<GetBlockCategoryByCriteria, BaseSearchResponse<GetBlockCategoryDto>>
    {
        private readonly IBlockCategoryService _service;

        public GetBlockCategoryByCriteriaHandler(IBlockCategoryService service)
        {
            _service = service;
        }

        public Task<BaseSearchResponse<GetBlockCategoryDto>> Handle(GetBlockCategoryByCriteria request, CancellationToken cancellationToken)
        {
            var filter = new SearchFilter("ParentId == null", "true", "eq", "boolean");
            request.Filter = JsonConvert.SerializeObject(new SearchAndFilter(new List<SearchFilter> { filter }, request.Filter));
            return _service.SearchAsync(request);
        }
    }
}
