using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.BlockFunctionCategory.Model;
using Device.Application.Constants;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command
{
    public class GetBlockCategoryByCriteria : BaseCriteria, IRequest<BaseSearchResponse<GetBlockCategoryDto>>
    {
        //public bool ClientOverride { get; set; } = false;
        public GetBlockCategoryByCriteria()
        {
            Sorts = DefaultSearchConstants.DEFAULT_SORT;
            Fields = new[] { "Id", "Name", "UpdatedUtc", "CreatedUtc"};
        }
    }
}
