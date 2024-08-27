using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.Service.FileImport.Abstraction;
using AHI.Infrastructure.Import;
using AHI.Infrastructure.Import.Abstraction;

namespace AHI.Device.Function.Service.FileImport
{
    public class UomImportService : BaseFileImport<Uom>, IUomImportService
    {
        private readonly IFileHandler<Uom> _fileHandler;
        private readonly IImportRepository<Uom> _repository;
        public UomImportService(IFileHandler<Uom> fileHandler, IImportRepository<Uom> repository)
        {
            _fileHandler = fileHandler;
            _repository = repository;
        }

        protected override IFileHandler<Uom> GetFileHandler()
        {
            return _fileHandler;
        }

        protected override IImportRepository<Uom> GetRepository()
        {
            return _repository;
        }
    }
}