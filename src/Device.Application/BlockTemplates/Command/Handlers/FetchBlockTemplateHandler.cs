using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockTemplate.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockTemplate.Command.Handler
{
    public class FetchBlockTemplateHandler : IRequestHandler<FetchBlockTemplate, FunctionBlockTemplateSimpleDto>
    {
        private readonly IFunctionBlockTemplateService _service;

        public FetchBlockTemplateHandler(IFunctionBlockTemplateService service)
        {
            _service = service;
        }

        public Task<FunctionBlockTemplateSimpleDto> Handle(FetchBlockTemplate request, CancellationToken cancellationToken)
        {
            return _service.FetchAsync(request.Id);
        }
    }
}