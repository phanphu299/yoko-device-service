
using System;
using Device.Application.Asset.Command.Model;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;

namespace Device.Application.Asset.Command
{
    public class UpsertAssetAttribute : IRequest<UpsertAssetAttributeDto>
    {
        public JsonPatchDocument Data { set; get; }

        public Guid AssetId { set; get; }
    }
}
