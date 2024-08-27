using System.Linq;
namespace Device.Application.Repository
{
    public interface IReadAssetAttributeSnapshotRepository
    {
        IQueryable<Domain.Entity.AttributeSnapshot> Snapshots { get; }
    }
}