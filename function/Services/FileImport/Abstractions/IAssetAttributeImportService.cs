using AHI.Device.Function.Model.ImportModel;
using AHI.Infrastructure.Import.Abstraction;

namespace AHI.Device.Function.Service.FileImport.Abstraction
{
    public interface IAssetAttributeImportService
    {
        public IFileHandler<AssetAttribute> GetFileHandler();
        public IImportRepository<AssetAttribute> GetRepository();
    }
}
