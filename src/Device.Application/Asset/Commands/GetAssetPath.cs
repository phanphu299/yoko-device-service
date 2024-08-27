using System;
using System.Collections.Generic;
using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class GetAssetPath : IRequest<IEnumerable<AssetPathDto>>
    {
        public IEnumerable<Guid> AssetIds { get; set; }
        public bool IncludeAttribute { get; set; }

        public GetAssetPath(IEnumerable<Guid> assetIds, bool includeAttribute)
        {
            AssetIds = assetIds;
            IncludeAttribute = includeAttribute;
        }
    }
}
