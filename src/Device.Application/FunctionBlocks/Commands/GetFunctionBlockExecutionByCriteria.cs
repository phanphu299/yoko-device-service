using Device.Application.BlockFunction.Model;
using Device.Application.Constants;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.BlockFunction.Query
{
    public class GetFunctionBlockExecutionByCriteria : BaseCriteria, IRequest<BaseSearchResponse<FunctionBlockExecutionDto>>
    {
        //public bool ClientOverride { get; set; } = false;
        public GetFunctionBlockExecutionByCriteria()
        {
            Sorts = DefaultSearchConstants.DEFAULT_SORT;
            Fields = new[] { "Id", "Name", "TriggerType", "TriggerContent", "TemplateId", "FunctionBlockId", "Status", "UpdatedUtc", "ExecutedUtc", "TemplateOverlay", "Version" };
        }
    }
}
