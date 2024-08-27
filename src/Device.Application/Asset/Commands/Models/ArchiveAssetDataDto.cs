using System.Collections.Generic;

namespace Device.Application.Asset.Command.Model
{
    public class ArchiveAssetDataDto
    {
        public IEnumerable<ArchiveAssetDto> Assets { get; set; }

        public ArchiveAssetDataDto(IEnumerable<ArchiveAssetDto> assets)
        {
            Assets = assets;
        }
    }
}
