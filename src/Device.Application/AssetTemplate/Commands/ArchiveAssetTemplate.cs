using System;
using Device.Application.AssetTemplate.Command.Model;
using MediatR;

namespace Device.Application.AssetTemplate.Command
{
    public class ArchiveAssetTemplate : IRequest<ArchiveAssetTemplateDataDto>
    {
        public DateTime ArchiveTime { get; set; } = DateTime.UtcNow;
    }
}
