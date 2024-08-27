using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Device.Application.Repository
{
    public interface IReadDeviceTemplateRepository : IReadRepository<Domain.Entity.DeviceTemplate, Guid>
    {
        Task<Domain.Entity.DeviceTemplate> FindEntityWithRelationAsync(Guid id);
        Task<bool> ValidationAttributeUsingMetricsAsync(Domain.Entity.DeviceTemplate template, string key);
        Task<bool> HasBindingAsync(Guid id);
    }
}