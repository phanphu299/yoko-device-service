using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;
using Device.Application.Service.Abstraction;
using System;

namespace Device.Application.FileRequest.Command.Handler
{
    public class ImportFileRequestHandler : IRequestHandler<ImportFile, BaseResponse>
    {
        IFileEventService _fileEventService;
        public ImportFileRequestHandler(IFileEventService fileEventService)
        {
            _fileEventService = fileEventService;
        }

        public async Task<BaseResponse> Handle(ImportFile request, CancellationToken cancellationToken)
        {
            Guid correlationId = Guid.NewGuid();
            await _fileEventService.SendImportEventAsync(request.ObjectType, request.FileNames, correlationId);
            return new BaseResponse(true, $"CorrelationId: {correlationId} | Starting import");
        }

    }
}
