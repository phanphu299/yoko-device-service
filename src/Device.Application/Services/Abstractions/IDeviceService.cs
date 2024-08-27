using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Asset.Command;
using Device.Application.Asset.Command.Model;
using Device.Application.Device.Command;
using Device.Application.Device.Command.Model;
using Device.Application.Models;

namespace Device.Application.Service.Abstraction
{
    public interface IDeviceService : ISearchService<Domain.Entity.Device, string, GetDeviceByCriteria, GetDeviceDto>, IFetchService<Domain.Entity.Device, string, GetDeviceDto>
    {
        Task<AddDeviceDto> AddAsync(AddDevice payload, CancellationToken token);
        Task<UpdateDeviceDto> UpdateAsync(UpdateDevice command, CancellationToken token);
        Task<MetricAssemblyDto> GenerateMetricAssemblyAsync(string deviceId, CancellationToken cancellationToken);
        Task<UpdateDeviceDto> PartialUpdateAsync(PatchDevice payload, CancellationToken token);
        Task<GetDeviceDto> FindByIdAsync(GetDeviceById payload, CancellationToken token);
        Task<BaseResponse> RemoveEntityForceAsync(DeleteDevice command, CancellationToken token);
        Task<IEnumerable<SnapshotDto>> GetMetricSnapshotAsync(GetMetricSnapshot request, CancellationToken cancellationToken);
        Task<IEnumerable<GetDeviceDto>> GetDevicesByTemplateIdAsync(GetDevicesByTemplateId request, CancellationToken cancellationToken);
        Task<ActivityResponse> ExportAsync(ExportDevice request, CancellationToken cancellationToken);
        Task<BaseResponse> PushConfigurationMessageAsync(PushMessageToDevice command, CancellationToken token);
        Task<BaseResponse> PushConfigurationMessageMutipleAsync(IEnumerable<PushMessageToDevice> commands, CancellationToken token);
        Task<IEnumerable<GetMetricsByDeviceIdDto>> GetMetricsByDeviceIdAsync(GetMetricsByDeviceId request, CancellationToken cancellationToken);
        Task<IEnumerable<SharingBrokerDto>> SearchSharingBrokerAsync(SearchSharingBroker command, CancellationToken cancellationToken);
        Task<BaseResponse> CheckExistDevicesAsync(CheckExistDevice command, CancellationToken cancellationToken);
        Task<bool> CheckExistMetricByDeviceIdAsync(string metricKey, string deviceId);
        Task<UpdateDeviceDto> RefreshTokenAsync(RefreshToken command, CancellationToken token);
        Task<BaseSearchResponse<GetDeviceDto>> FindDeviceHasBinding(GetDeviceHasBinding payload, CancellationToken cancellationToken);
        Task<FetchDeviceMetricDto> FetchDeviceMetricAsync(string deviceId, int metricId, string metricKey);
        Task<IEnumerable<ArchiveDeviceDto>> ArchiveAsync(ArchiveDevice command, CancellationToken cancellationToken);
        Task<BaseResponse> RetrieveAsync(RetrieveDevice command, CancellationToken cancellationToken);
        Task<BaseResponse> VerifyArchiveAsync(VerifyDevice command, CancellationToken cancellationToken);
    }
}
