
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Repository;
using Microsoft.EntityFrameworkCore;
using MediatR;
namespace Device.Application.Device.Command.Handler
{
    public class GetDeviceMetricSnapshotRequestHandler : IRequestHandler<GetDeviceSignalSnapshot, DeviceSignalSnapshotDto>
    {
        private readonly IReadDeviceMetricSnapshotRepository _readDeviceMetricSnapshotRepository;

        public GetDeviceMetricSnapshotRequestHandler(IReadDeviceMetricSnapshotRepository readDeviceMetricSnapshotRepository)
        {
            _readDeviceMetricSnapshotRepository = readDeviceMetricSnapshotRepository;
        }

        public async Task<DeviceSignalSnapshotDto> Handle(GetDeviceSignalSnapshot request, CancellationToken cancellationToken)
        {
            var deviceSnapshot = await _readDeviceMetricSnapshotRepository.DeviceSignalSnapshots.AsNoTracking()
                                                                        .Where(x => x.DeviceId == request.DeviceId && x.MetricId == request.MetricId)
                                                                        .OrderByDescending(x => x.UpdatedUtc)
                                                                        .FirstOrDefaultAsync();
            return DeviceSignalSnapshotDto.Create(deviceSnapshot);
        }
    }
}