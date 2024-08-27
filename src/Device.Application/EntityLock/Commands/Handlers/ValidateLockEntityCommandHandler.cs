using System.Threading;
using System.Threading.Tasks;
using Device.Application.EntityLock.Command;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class ValidateLockEntityCommandHandler : IRequestHandler<ValidateLockEntityCommand, bool>
    {
        private readonly IEntityLockService _service;


        public ValidateLockEntityCommandHandler(IEntityLockService service)
        {
            _service = service;
        }

        public Task<bool> Handle(ValidateLockEntityCommand request, CancellationToken cancellationToken)
        {
            return _service.ValidateEntityLockedByOtherAsync(request, cancellationToken);
        }
    }
}
