using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.BlockCategory.Model;
using MediatR;

namespace Device.Application.BlockCategory.Command
{
    public class GetBlockCategoryHierarchy : BaseCriteria, IRequest<BaseSearchResponse<GetBlockCategoryHierarchyDto>>
    {
        public string Name { get; set; }
    }
}
