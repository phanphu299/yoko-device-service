using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.Service.FileImport.Abstraction;
using AHI.Infrastructure.Import;
using AHI.Infrastructure.Import.Abstraction;

namespace AHI.Device.Function.Service.FileImport
{
    public class AssetTemplateImportService : BaseFileImport<AssetTemplate>, IAssetTemplateImportService
    {
        private readonly IFileHandler<AssetTemplate> _fileHandler;
        private readonly IImportRepository<AssetTemplate> _repository;
        public AssetTemplateImportService(IFileHandler<AssetTemplate> fileHandler, IImportRepository<AssetTemplate> repository)
        {
            _fileHandler = fileHandler;
            _repository = repository;
        }

        protected override IFileHandler<AssetTemplate> GetFileHandler()
        {
            return _fileHandler;
        }

        protected override IImportRepository<AssetTemplate> GetRepository()
        {
            return _repository;
        }
    }
}