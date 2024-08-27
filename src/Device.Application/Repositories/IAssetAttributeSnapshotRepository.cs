using System.Linq;

namespace Device.Application.Repository
{
    public interface IAssetAttributeSnapshotRepository
    {
        IQueryable<Domain.Entity.AttributeSnapshot> Snapshots { get; }
    }
}