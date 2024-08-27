using Device.Application.Constants;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;
using Device.Application.BlockTemplate.Command.Model;

namespace Device.Application.BlockTemplate.Command
{
    public class GetFunctionBlockTemplateByCriteria : BaseCriteria, IRequest<BaseSearchResponse<FunctionBlockTemplateSimpleDto>>
    {
        //public bool ClientOverride { get; set; } = false;
        public GetFunctionBlockTemplateByCriteria()
        {
            Sorts = DefaultSearchConstants.DEFAULT_SORT;
        }
    }
}
