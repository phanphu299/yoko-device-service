using System.Linq;
namespace Device.Application.Repository
{
    public interface IReadDeviceMetricSnapshotRepository
    {
        IQueryable<Domain.Entity.DeviceMetricSnapshot> DeviceSignalSnapshots { get; }
    }
}