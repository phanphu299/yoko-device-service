using System.Linq;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;

namespace Device.Persistence.Repository
{
    public class AssetAttributeSnapshotRepository : IAssetAttributeSnapshotRepository, IReadAssetAttributeSnapshotRepository
    {
        private readonly DeviceDbContext _dbContext;

        public AssetAttributeSnapshotRepository(DeviceDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IQueryable<AttributeSnapshot> Snapshots => _dbContext.AssetAttributeSnapshots;
    }
}