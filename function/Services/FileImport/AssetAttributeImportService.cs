using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.Service.FileImport.Abstraction;
using AHI.Infrastructure.Import.Abstraction;

namespace AHI.Device.Function.Service.FileImport
{
    public class AssetAttributeImportService : IAssetAttributeImportService
    {
        private readonly IFileHandler<AssetAttribute> _fileHandler;
        private readonly IImportRepository<AssetAttribute> _repository;
        public AssetAttributeImportService(IFileHandler<AssetAttribute> fileHandler, IImportRepository<AssetAttribute> repository)
        {
            _fileHandler = fileHandler;
            _repository = repository;
        }

        public IFileHandler<AssetAttribute> GetFileHandler()
        {
            return _fileHandler;
        }

        public IImportRepository<AssetAttribute> GetRepository()
        {
            return _repository;
        }
    }
}
