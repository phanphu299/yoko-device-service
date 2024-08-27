using System.Threading;
using System.Threading.Tasks;
using Device.Application.Template.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using AHI.Infrastructure.Validation.Abstraction;
using System.Linq;

namespace Device.Application.Template.Command.Handler
{
    public class AddTemplateRequestHandler : IRequestHandler<AddTemplates, AddTemplatesDto>
    {
        #region Properties

        private readonly IDeviceTemplateService _templateService;

        private readonly IDynamicValidator _dynamicValidator;

        #endregion

        #region Constructor

        public AddTemplateRequestHandler(IDeviceTemplateService templateService, IDynamicValidator dynamicValidator)
        {
            _templateService = templateService;
            _dynamicValidator = dynamicValidator;
        }

        #endregion

        #region Methods

        public virtual async Task<AddTemplatesDto> Handle(AddTemplates request, CancellationToken cancellationToken)
        {
            // Get the payload and do dynamic validation.
            if (request.Payloads.Any())
            {
                foreach (var payload in request.Payloads)
                {
                    await _dynamicValidator.ValidateAsync(payload, cancellationToken);

                    if (payload.Details.Any())
                    {
                        foreach (var detailedPayload in payload.Details)
                        {
                            await _dynamicValidator.ValidateAsync(detailedPayload, cancellationToken);
                        }
                    }
                }
            }

            // persist to database and implement extra logic
            return await _templateService.AddEntityAsync(request, cancellationToken);
        }

        #endregion
    }
}
