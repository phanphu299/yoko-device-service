using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.Service.FileImport.Abstraction;
using AHI.Infrastructure.Import;
using AHI.Infrastructure.Import.Abstraction;

namespace AHI.Device.Function.Service.FileImport
{
    public class DeviceImportService : BaseFileImport<DeviceModel>, IDeviceImportService
    {
        private readonly IFileHandler<DeviceModel> _fileHandler;
        private readonly IImportRepository<DeviceModel> _repository;

        public DeviceImportService(IFileHandler<DeviceModel> fileHandler, IImportRepository<DeviceModel> repository)
        {
            _fileHandler = fileHandler;
            _repository = repository;
        }

        protected override IFileHandler<DeviceModel> GetFileHandler()
        {
            return _fileHandler;
        }

        protected override IImportRepository<DeviceModel> GetRepository()
        {
            return _repository;
        }

    }
}