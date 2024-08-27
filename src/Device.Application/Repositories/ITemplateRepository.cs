using System.Threading.Tasks;
using AHI.Infrastructure.Repository.Generic;
using System;
using System.Collections.Generic;

namespace Device.Application.Repository
{
    public interface IDeviceTemplateRepository : IRepository<Domain.Entity.DeviceTemplate, Guid>
    {
        Task<Domain.Entity.DeviceTemplate> AddEntityWithRelationAsync(Domain.Entity.DeviceTemplate e);
        Task<Domain.Entity.DeviceTemplate> UpdateEntityWithRelationAsync(Guid key, Domain.Entity.DeviceTemplate updateTemplate);
        Task<Domain.Entity.DeviceTemplate> FindEntityWithRelationAsync(Guid id);
        Task<bool> ValidationAttributeUsingMetricsAsync(Domain.Entity.DeviceTemplate template, string key);
        Task<bool> HasBindingAsync(Guid id);
        Task RetrieveAsync(IEnumerable<Domain.Entity.DeviceTemplate> input);
    }
}
