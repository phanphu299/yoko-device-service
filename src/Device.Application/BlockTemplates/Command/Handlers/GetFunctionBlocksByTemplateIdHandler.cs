using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Block.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockTemplate.Command.Handler
{
    public class GetFunctionBlocksByTemplateIdHandler : IRequestHandler<GetFunctionBlockByTemplateId, IEnumerable<GetFunctionBlockDto>>
    {
        private readonly IFunctionBlockTemplateService _service;
        public GetFunctionBlocksByTemplateIdHandler(IFunctionBlockTemplateService service)
        {
            _service = service;
        }

        public Task<IEnumerable<GetFunctionBlockDto>> Handle(GetFunctionBlockByTemplateId request, CancellationToken cancellationToken)
        {
            return _service.GetFunctionBlocksByTemplateIdAsync(request, cancellationToken);
        }
    }
}
