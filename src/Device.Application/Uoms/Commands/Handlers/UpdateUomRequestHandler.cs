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
    public class UpdateUomRequestHandler : IRequestHandler<UpdateUom, UpdateUomsDto>
    {
        #region Properties

        private readonly IUomService _uomService;

        private readonly IConfigurationService _configurationService;

        private readonly IAuditLogService _auditLogService;

        #endregion

        #region Constructor

        public UpdateUomRequestHandler(IUomService uomService,
            IConfigurationService configurationService,
            IAuditLogService auditLogService)
        {
            _uomService = uomService;
            _configurationService = configurationService;
            _auditLogService = auditLogService;
        }

        #endregion

        #region Methods

        public virtual async Task<UpdateUomsDto> Handle(UpdateUom request, CancellationToken cancellationToken)
        {
            // Check the validity of lookup code.
            var lookupCode = request.LookupCode;
            var lookup = await _configurationService.FindLookupByCodeAsync(lookupCode, cancellationToken);
            if (lookup == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(AddUom.LookupCode));

            // Lookup is not active.
            if (!lookup.Active)
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(AddUom.LookupCode));

            try
            {
                var result = await _uomService.UpdateUomAsync(request, cancellationToken);
                await _auditLogService.SendLogAsync(ActivityEntityAction.UOM, ActionType.Update, ActionStatus.Success, request.Id, request.Name, request);
                return result;
            }
            catch
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.UOM, ActionType.Update, ActionStatus.Fail, request.Id, request.Name, payload: request);
                throw;
            }

        }

        #endregion
    }
}
