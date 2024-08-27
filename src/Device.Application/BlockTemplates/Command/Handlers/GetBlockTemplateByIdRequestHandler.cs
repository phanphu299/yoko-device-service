using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockTemplate.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockTemplate.Query.Handler
{
    class GetBlockTemplateByIdRequestHandle : IRequestHandler<GetBlockTemplateById, GetFunctionBlockTemplateDto>
    {
        private readonly IFunctionBlockTemplateService _service;
        public GetBlockTemplateByIdRequestHandle(IFunctionBlockTemplateService service)
        {
            _service = service;
        }

        public Task<GetFunctionBlockTemplateDto> Handle(GetBlockTemplateById request, CancellationToken cancellationToken)
        {
            return _service.FindBlockTemplateByIdAsync(request, cancellationToken);
        }
    }
}
