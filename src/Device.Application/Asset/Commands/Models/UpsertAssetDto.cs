using System.Collections.Generic;
using Device.Application.SharedKernel;
namespace Device.Application.Asset.Command.Model
{
    public class UpsertAssetDto
    {
        public List<BaseJsonPathDocument> Data { set; get; } = new List<BaseJsonPathDocument>();
    }
}