using System.Threading.Tasks;
using AHI.Device.Function.Model;
using AHI.Device.Function.Model.ImportModel;
using Microsoft.Azure.WebJobs;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IAssetAttributeParseService
    {
        Task<AssetAttributeImportResponse> ParseAsync(ParseAssetAttributeMessage message, ExecutionContext context);
    }
}
