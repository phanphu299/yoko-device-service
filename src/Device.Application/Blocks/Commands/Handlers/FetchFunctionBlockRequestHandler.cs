using System.Threading;
using System.Threading.Tasks;
using Device.Application.Block.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Block.Command.Handler
{
    public class FetchFunctionBlockRequestHandler : IRequestHandler<FetchFunctionBlock, GetFunctionBlockSimpleDto>
    {
        private readonly IFunctionBlockService _service;

        public FetchFunctionBlockRequestHandler(IFunctionBlockService service)
        {
            _service = service;
        }

        public Task<GetFunctionBlockSimpleDto> Handle(FetchFunctionBlock request, CancellationToken cancellationToken)
        {
            return _service.FetchAsync(request.Id);
        }
    }
}