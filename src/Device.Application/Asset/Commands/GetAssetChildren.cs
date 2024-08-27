
using Device.Application.Asset.Command.Model;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;
using System;

namespace Device.Application.Asset.Command
{
    public class GetAssetChildren : BaseCriteria, IRequest<BaseSearchResponse<GetAssetSimpleDto>>
    {
        public Guid AssetId { get; set; }
        public GetAssetChildren(Guid id)
        {
            AssetId = id;
        }
    }
}
