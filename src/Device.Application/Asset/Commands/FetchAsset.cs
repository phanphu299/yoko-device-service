using System;
using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class FetchAsset : IRequest<GetAssetSimpleDto>
    {
        public Guid Id { get; set; }
        public FetchAsset(Guid id)
        {
            Id = id;
        }
    }
}