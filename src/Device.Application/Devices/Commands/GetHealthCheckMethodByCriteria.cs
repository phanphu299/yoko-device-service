using Device.Application.Device.Command.Model;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Device.Command
{
    public class GetHealthCheckMethodByCriteria : BaseCriteria, IRequest<BaseSearchResponse<GetHealthCheckMethodDto>>
    {
    }
}
