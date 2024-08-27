using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.Models;

namespace Device.Application.Repository
{
    public interface IReadDeviceRepository : IReadRepository<Domain.Entity.Device, string>
    {
        Task<bool> IsDuplicateDeviceIdAsync(string id);

        Task<IEnumerable<Domain.Entity.DeviceMetricSnapshotInfo>> GetMetricSnapshotAsync(string id);

        Task<IEnumerable<Domain.Entity.Device>> GetDevicesByTemplateIdAsync(Guid templateId);

        Task<IEnumerable<string>> ValidateDeviceIdsAsync(string[] deviceIds, bool includeDeleted = false);

        Task<int> GetTotalDeviceAsync();

        Task<DeviceInformation> GetDeviceInformationAsync(string projectId, string deviceId);
    }
}
