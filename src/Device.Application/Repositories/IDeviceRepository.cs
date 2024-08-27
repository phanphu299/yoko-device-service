using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Repository.Generic;
using Device.Application.Device.Command;
using Device.Application.Device.Command.Model;

namespace Device.Application.Repository
{
    public interface IDeviceRepository : IRepository<Domain.Entity.Device, string>
    {
        Task<bool> RemoveEntityWithRelationAsync(string id);

        Task<bool> IsDuplicateDeviceIdAsync(string id);

        Task<bool> RemoveListEntityWithRelationAsync(ICollection<Domain.Entity.Device> devices);

        Task<IEnumerable<Domain.Entity.DeviceMetricSnapshotInfo>> GetMetricSnapshotAsync(string id);

        Task<IEnumerable<Domain.Entity.Device>> GetDevicesByTemplateIdAsync(Guid templateId);

        Task<IEnumerable<string>> ValidateDeviceIdsAsync(string[] deviceIds, bool includeDeleted = false);

        Task<int> GetTotalDeviceAsync();

        Task RetrieveAsync(IEnumerable<Domain.Entity.Device> devices);

        Task UpdateDeviceRelationNavigationsAsync(string oldDeviceId, Domain.Entity.Device newDevice);

        Task<IEnumerable<GetDeviceDto>> GetDeviceAsync(GetDeviceByCriteria criteria);
        Task<int> CountAsync(GetDeviceByCriteria criteria);
    }
}
