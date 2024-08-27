using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Block.Command.Model;
using Device.Application.Constants;
using MediatR;

namespace Device.Application.Block.Command
{
    public class GetFunctionBlockByCriteria : BaseCriteria, IRequest<BaseSearchResponse<GetFunctionBlockSimpleDto>>
    {

        //public bool ClientOverride { get; set; } = false;
        public GetFunctionBlockByCriteria()
        {
            Sorts = DefaultSearchConstants.DEFAULT_SORT;
            Fields = new[] { "Id", "Name", "Deleted", "BlockContent", "CategoryId", "Type", "UpdatedUtc", "CreatedUtc", "Bindings" };
        }
    }
}
