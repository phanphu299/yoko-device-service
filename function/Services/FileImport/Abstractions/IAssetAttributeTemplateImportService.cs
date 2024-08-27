using AHI.Device.Function.Model.ImportModel.Attribute;
using AHI.Infrastructure.Import.Abstraction;

namespace AHI.Device.Function.Service.FileImport.Abstraction
{
    public interface IAssetAttributeTemplateImportService
    {
        IFileHandler<AttributeTemplate> GetFileHandler();
        IImportRepository<AttributeTemplate> GetRepository();
    }
}
