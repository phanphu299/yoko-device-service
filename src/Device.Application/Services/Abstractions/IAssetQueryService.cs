using System.Threading;
using System.Threading.Tasks;
using Device.Application.Asset.Command;
using Device.Application.BlockFunction.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IAssetQueryService
    {
        Task<BlockQueryResult> QueryAsync(AssetAttributeQuery command, CancellationToken cancellationToken);
    }
}