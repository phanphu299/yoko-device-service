using Device.Application.Constants;
using Device.Application.Uom.Command.Model;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Uom.Command
{
    public class GetUomByCriteria : BaseCriteria, IRequest<BaseSearchResponse<GetUomDto>>
    {
        //public bool ClientOverride { get; set; } = false;

        public GetUomByCriteria()
        {
            Sorts = DefaultSearchConstants.DEFAULT_SORT;
        }
    }
}
