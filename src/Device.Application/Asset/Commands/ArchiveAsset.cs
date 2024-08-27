using System;
using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class ArchiveAsset : IRequest<ArchiveAssetDataDto>
    {
        public DateTime ArchiveTime { get; set; } = DateTime.UtcNow;
    }
}
