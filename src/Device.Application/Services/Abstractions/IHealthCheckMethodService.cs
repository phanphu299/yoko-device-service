
using AHI.Infrastructure.Service.Abstraction;
using Device.Application.Device.Command;
using Device.Application.Device.Command.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IHealthCheckMethodService : ISearchService<Domain.Entity.HealthCheckMethod, int, GetHealthCheckMethodByCriteria, GetHealthCheckMethodDto>
    {
    }
}
