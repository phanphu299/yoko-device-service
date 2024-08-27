using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.Service.FileImport.Abstraction;
using AHI.Infrastructure.Import;
using AHI.Infrastructure.Import.Abstraction;

namespace AHI.Device.Function.Service.FileImport
{
    public class DeviceTemplateImportService : BaseFileImport<DeviceTemplate>, IDeviceTemplateImportService
    {
        private readonly IFileHandler<DeviceTemplate> _fileHandler;
        private readonly IImportRepository<DeviceTemplate> _repository;

        public DeviceTemplateImportService(IFileHandler<DeviceTemplate> fileHandler, IImportRepository<DeviceTemplate> repository)
        {
            _fileHandler = fileHandler;
            _repository = repository;
        }

        protected override IFileHandler<DeviceTemplate> GetFileHandler()
        {
            return _fileHandler;
        }

        protected override IImportRepository<DeviceTemplate> GetRepository()
        {
            return _repository;
        }
    }
}