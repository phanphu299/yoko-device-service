using System;
using System.Threading.Tasks;
using AHI.Device.Function.Model;
namespace AHI.Infrastructure.Repository.Abstraction.ReadOnly
{
    public interface IReadOnlyAssetRepository
    {
        Task<AssetInformation> GetAssetInformationsAsync(string projectId, Guid assetId);
    }
}