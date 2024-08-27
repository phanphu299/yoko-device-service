using System.Collections.Generic;
using Device.Application.SharedKernel;

namespace Device.Application.Asset.Command.Model
{
    public class UpsertAssetAttributeDto
    {
        public IList<BaseJsonPathDocument> Data { set; get; }
        public UpsertAssetAttributeDto()
        {
            Data = new List<BaseJsonPathDocument>();
        }
    }
}
