using Device.Application.Constants;
using Device.Application.Template.Command.Model;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.AssetAttribute.Command
{
    public class GetValidTemplatesByCriteria : BaseCriteria, IRequest<BaseSearchResponse<GetValidTemplateDto>>
    {
        //public bool ClientOverride { get; set; } = false;
        public GetValidTemplatesByCriteria()
        {
            Sorts = DefaultSearchConstants.DEFAULT_SORT;
        }
    }
}
