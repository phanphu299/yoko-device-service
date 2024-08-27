using System.Collections.Generic;
using AHI.Device.Function.Model.ImportModel.Attribute;

namespace AHI.Device.Function.Model.ImportModel
{
    public class AssetAttributeImportResponse
    {
        public IEnumerable<AssetAttribute> Attributes { get; set; } = new List<AssetAttribute>();
        public IEnumerable<ErrorDetail> Errors { get; set; }

        public AssetAttributeImportResponse()
        {
            Attributes = new List<AssetAttribute>();
            Errors = new List<ErrorDetail>();
        }
    }
}
