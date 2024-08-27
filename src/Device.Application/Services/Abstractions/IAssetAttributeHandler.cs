using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Device.Application.Service.Abstraction
{
    public interface IAssetAttributeHandler
    {
        bool CanApply(string attributeType);
        Task<Domain.Entity.AssetAttribute> AddAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken, bool ignoreValidation = false);
        Task<Domain.Entity.AssetAttribute> UpdateAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken);
    }
}