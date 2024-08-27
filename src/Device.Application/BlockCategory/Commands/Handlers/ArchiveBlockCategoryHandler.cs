using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockFunctionCategory.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command.Handler
{
    public class ArchiveBlockCategoryHandler : IRequestHandler<ArchiveBlockCategory, IEnumerable<ArchiveBlockCategoryDto>>
    {
        private readonly IBlockCategoryService _service;

        public ArchiveBlockCategoryHandler(IBlockCategoryService service)
        {
            _service = service;
        }

        public Task<IEnumerable<ArchiveBlockCategoryDto>> Handle(ArchiveBlockCategory request, CancellationToken cancellationToken)
        {
            return _service.ArchiveAsync(request, cancellationToken);
        }
    }
}