using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.BlockSnippet.Model;
using Device.Application.Constants;
using MediatR;

namespace Device.Application.BlockSnippet.Command
{
    public class GetBlockSnippetByCriteria : BaseCriteria, IRequest<BaseSearchResponse<BlockSnippetDto>>
    {
        //public bool ClientOverride { get; set; } = false;
        public GetBlockSnippetByCriteria()
        {
            Sorts = DefaultSearchConstants.DEFAULT_SORT_CREATED_UTC;
            Fields = new[] { "Id", "Name", "TemplateCode", "UpdatedUtc", "CreatedUtc" };
        }
    }
}
