using System;
using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class GetAssetClone : IRequest<GetAssetDto>
    {
        public Guid Id { get; set; }
        public bool IncludeChildren { get; set; }
        public GetAssetClone(Guid id, bool includeChildren)
        {
            Id = id;
            IncludeChildren = includeChildren;
        }
    }
}
