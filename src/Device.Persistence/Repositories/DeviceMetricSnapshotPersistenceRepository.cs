using System.Linq;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;
namespace Device.Persistence.Repository
{
    public class DeviceMetricSnapshotPersistenceRepository : IDeviceMetricSnapshotRepository, IReadDeviceMetricSnapshotRepository
    {
        private readonly DeviceDbContext _context;

        public DeviceMetricSnapshotPersistenceRepository(DeviceDbContext context)
        {
            _context = context;
        }

        public IQueryable<DeviceMetricSnapshot> DeviceSignalSnapshots => _context.DeviceSignalSnapshots;
    }
}