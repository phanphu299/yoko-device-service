using System;
using System.Collections.Generic;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class CheckExistingAssetIds : IRequest<IEnumerable<Guid>>
    {
        public IEnumerable<Guid> AssetIds { get; set; }

        public CheckExistingAssetIds(IEnumerable<Guid> assetIds)
        {
            AssetIds = assetIds;
        }
    }
}
