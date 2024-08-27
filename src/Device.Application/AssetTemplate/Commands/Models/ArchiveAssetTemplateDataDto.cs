using System.Collections.Generic;

namespace Device.Application.AssetTemplate.Command.Model
{
    public class ArchiveAssetTemplateDataDto
    {
        public IEnumerable<ArchiveAssetTemplateDto> AssetTemplates { get; set; }

        public ArchiveAssetTemplateDataDto(IEnumerable<ArchiveAssetTemplateDto> assetTemplates)
        {
            AssetTemplates = assetTemplates;
        }
    }
}
