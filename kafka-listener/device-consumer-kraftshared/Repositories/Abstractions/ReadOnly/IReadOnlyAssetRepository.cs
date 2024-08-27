using System;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Model;
namespace Device.Consumer.KraftShared.Repositories.Abstraction.ReadOnly
{
    public interface IReadOnlyAssetRepository
    {
        Task<AssetInformation> GetAssetInformationsAsync(string projectId, Guid assetId);
    }
}