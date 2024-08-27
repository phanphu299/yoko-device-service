using AHI.Device.Function.Model.ImportModel.Attribute;
using AHI.Device.Function.Service.FileImport.Abstraction;
using AHI.Infrastructure.Import.Abstraction;

namespace AHI.Device.Function.Service.FileImport
{
    public class AssetAttributeTemplateImportService : IAssetAttributeTemplateImportService
    {
        private readonly IFileHandler<AttributeTemplate> _fileHandler;
        private readonly IImportRepository<AttributeTemplate> _repository;
        public AssetAttributeTemplateImportService(IFileHandler<AttributeTemplate> fileHandler, IImportRepository<AttributeTemplate> repository)
        {
            _fileHandler = fileHandler;
            _repository = repository;
        }

        public IFileHandler<AttributeTemplate> GetFileHandler()
        {
            return _fileHandler;
        }

        public IImportRepository<AttributeTemplate> GetRepository()
        {
            return _repository;
        }
    }
}
