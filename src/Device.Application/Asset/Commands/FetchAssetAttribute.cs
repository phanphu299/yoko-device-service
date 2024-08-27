using System;
using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class FetchAssetAttribute : IRequest<FetchAssetAttributeDto>
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }

        public FetchAssetAttribute(Guid id, Guid assetId)
        {
            Id = id;
            AssetId = assetId;
        }
    }
}