using Device.Application.Block.Command.Model;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;

namespace Device.Application.Block.Command
{
    public class UpsertFunctionBlock : IRequest<UpsertFunctionBlockDto>
    {
        public JsonPatchDocument Data { set; get; }
        public UpsertFunctionBlock(JsonPatchDocument data)
        {
            Data = data;
        }
    }
}
