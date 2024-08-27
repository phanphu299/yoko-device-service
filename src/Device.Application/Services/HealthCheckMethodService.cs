
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.Service;
using System;
using Device.Application.Device.Command.Model;
using Device.Application.Device.Command;

namespace Device.Application.Service
{
    public class HealthCheckMethodService : BaseSearchService<Domain.Entity.HealthCheckMethod, int, GetHealthCheckMethodByCriteria, GetHealthCheckMethodDto>, IHealthCheckMethodService
    {
        public HealthCheckMethodService(IServiceProvider serviceProvider)
            : base(GetHealthCheckMethodDto.Create, serviceProvider)
        {
        }

        protected override Type GetDbType()
        {
            return typeof(IHealthCheckMethodRepository);
        }
    }
}
