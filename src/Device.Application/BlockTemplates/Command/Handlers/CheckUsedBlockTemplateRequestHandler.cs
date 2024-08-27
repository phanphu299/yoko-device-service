using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockTemplate.Query.Handler
{
    class CheckUsedBlockTemplateRequestHandler : IRequestHandler<CheckUsedBlockTemplate, BaseResponse>
    {
        private readonly IFunctionBlockTemplateService _service;

        public CheckUsedBlockTemplateRequestHandler(IFunctionBlockTemplateService service)
        {
            _service = service;
        }

        public async Task<BaseResponse> Handle(CheckUsedBlockTemplate request, CancellationToken cancellationToken)
        {
            var result = await _service.CheckUsedBlockTemplateAsync(request, cancellationToken);
            return new BaseResponse(result, null);
        }
    }
}