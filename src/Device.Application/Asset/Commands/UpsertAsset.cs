using Device.Application.Asset.Command.Model;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;

namespace Device.Application.Asset.Command
{
    public class UpsertAsset : IRequest<UpsertAssetDto>
    {
        public JsonPatchDocument Data { set; get; }
        public UpsertAsset(JsonPatchDocument data)
        {
            Data = data;
        }
    }
}
