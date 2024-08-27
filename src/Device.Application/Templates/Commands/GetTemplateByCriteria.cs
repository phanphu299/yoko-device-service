using Device.Application.Constants;
using Device.Application.Template.Command.Model;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Template.Command
{
    public class GetTemplateByCriteria : BaseCriteria, IRequest<BaseSearchResponse<GetTemplateDto>>
    {
        public GetTemplateByCriteria()
        {
            Sorts = DefaultSearchConstants.DEFAULT_SORT;
        }
    }
}
