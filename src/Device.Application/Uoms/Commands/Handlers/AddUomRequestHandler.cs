using System.Threading;
using System.Threading.Tasks;
using Device.Application.Constant;
using Device.Application.Uom.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Audit.Constant;

namespace Device.Application.Uom.Command.Handler
{
    public class AddUomRequestHandler : IRequestHandler<AddUom, AddUomsDto>
    {
        #region Properties

        private readonly IUomService _service;

        private readonly IAuditLogService _auditLogService;

        private readonly IConfigurationService _configurationService;

        #endregion

        #region Constructor

        public AddUomRequestHandler(IUomService service, IAuditLogService auditLogService, IConfigurationService configurationService)
        {
            _service = service;
            _auditLogService = auditLogService;
            _configurationService = configurationService;
        }

        #endregion

        #region Methods

        public virtual async Task<AddUomsDto> Handle(AddUom request, CancellationToken cancellationToken)
        {
            try
            {
                // Check the validity of lookup code.
                var lookupCode = request.LookupCode;
                var lookup = await _configurationService.FindLookupByCodeAsync(lookupCode, cancellationToken);
                if (lookup == null)
                    throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(AddUom.LookupCode));

                // Lookup is not active.
                if (!lookup.Active)
                    throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(AddUom.LookupCode));

                // persist to database and implement extra logic
                var addedUom = await _service.AddUomAsync(request, cancellationToken);
                await _auditLogService.SendLogAsync(ActivityEntityAction.UOM, ActionType.Add, ActionStatus.Success, addedUom.Id, request.Name, request);
                return addedUom;
            }
            catch
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.UOM, ActionType.Add, ActionStatus.Fail, payload: request);
                throw;
            }
        }

        #endregion
    }
}
