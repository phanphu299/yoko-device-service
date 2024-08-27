
using Device.Application.Constants;
using Device.Application.AssetTemplate.Command.Model;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.AssetTemplate.Command
{
    public class GetAssetTemplateByCriteria : BaseCriteria, IRequest<BaseSearchResponse<GetAssetTemplateDto>>
    {
        //public bool ClientOverride { get; set; } = false;
        public GetAssetTemplateByCriteria()
        {
            Sorts = DefaultSearchConstants.DEFAULT_SORT;
        }
    }
}
