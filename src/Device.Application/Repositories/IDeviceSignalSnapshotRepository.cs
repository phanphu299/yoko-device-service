using System.Linq;

namespace Device.Application.Repository
{
    public interface IDeviceMetricSnapshotRepository
    {
        IQueryable<Domain.Entity.DeviceMetricSnapshot> DeviceSignalSnapshots { get; }
    }
}