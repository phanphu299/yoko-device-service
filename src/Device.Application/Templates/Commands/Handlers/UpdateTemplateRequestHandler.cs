using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Template.Command.Model;
using Device.Application.Service.Abstraction;
using Device.Application.TemplateDetail.Command;
using Device.Application.TemplatePayload.Command;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using AHI.Infrastructure.Validation.Abstraction;
using AHI.Infrastructure.Exception.Helper;

namespace Device.Application.Template.Command.Handler
{
    public class UpdateTemplateRequestHandler : IRequestHandler<UpdateTemplates, UpdateTemplatesDto>
    {
        private readonly IServiceProvider _serviceProvider;

        public UpdateTemplateRequestHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public virtual async Task<UpdateTemplatesDto> Handle(UpdateTemplates request, CancellationToken cancellationToken)
        {
            var payloadValidator = _serviceProvider.GetService<IValidator<UpdateTemplatePayload>>();
            var payloadDetailValidator = _serviceProvider.GetService<IValidator<UpdateTemplateDetails>>();
            var templateService = _serviceProvider.GetService<IDeviceTemplateService>();
            var dynamicValidator = _serviceProvider.GetService<IDynamicValidator>();

            if (request.Payloads != null && request.Payloads.Any())
            {
                await ValidatePayloadsAsync(request, payloadValidator, payloadDetailValidator, dynamicValidator, cancellationToken);
            }

            // persist to database and implement extra logic
            return await templateService.UpdateEntityAsync(request, cancellationToken);
        }

        private async Task ValidatePayloadsAsync(
            UpdateTemplates request,
            IValidator<UpdateTemplatePayload> payloadValidator,
            IValidator<UpdateTemplateDetails> payloadDetailValidator,
            IDynamicValidator dynamicValidator,
            CancellationToken cancellationToken
        )
        {
            foreach (var payload in request.Payloads)
            {
                var resultPayload = await payloadValidator.ValidateAsync(payload, cancellationToken);

                if (!resultPayload.IsValid)
                    throw EntityValidationExceptionHelper.GenerateException(resultPayload.Errors.ToList());

                // Do dynamic validator.
                await dynamicValidator.ValidateAsync(payload, cancellationToken);

                var details = payload.Details;
                if (details == null || !details.Any())
                    continue;

                foreach (var detail in details)
                {
                    var detailValidationResult = await payloadDetailValidator.ValidateAsync(detail, cancellationToken);
                    if (!detailValidationResult.IsValid)
                        throw EntityValidationExceptionHelper.GenerateException(detailValidationResult.Errors.ToList());

                    // Do dynamic validator.
                    await dynamicValidator.ValidateAsync(detail, cancellationToken);
                }
            }
        }
    }
}
