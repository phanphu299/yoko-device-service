using System.Threading;
using System.Threading.Tasks;
using Device.Application.EntityLock.Command;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class ValidateLockEntitiesCommandHandler : IRequestHandler<ValidateLockEntitiesCommand, bool>
    {
        private readonly IEntityLockService _service;


        public ValidateLockEntitiesCommandHandler(IEntityLockService service)
        {
            _service = service;
        }

        public Task<bool> Handle(ValidateLockEntitiesCommand request, CancellationToken cancellationToken)
        {
            return _service.ValidateEntitiesLockedByOtherAsync(request, cancellationToken);
        }
    }
}
