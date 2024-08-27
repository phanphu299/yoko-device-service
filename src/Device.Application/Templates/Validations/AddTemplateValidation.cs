using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Service.Tag.Model;
using Device.Application.Template.Command;
using Device.Application.TemplateBinding.Command;
using Device.Application.TemplateDetail.Command;
using Device.Application.TemplatePayload.Command;
using FluentValidation;
using FluentValidation.Results;

namespace Device.Application.Template.Validation
{
    public class AddTemplateValidation : AbstractValidator<AddTemplates>
    {
        private readonly IServiceProvider _serviceProvider;

        public AddTemplateValidation(IServiceProvider serviceProvider)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);

            RuleForEach(x => x.Tags).SetValidator(
                new InlineValidator<UpsertTag> {
                    agValidator => agValidator.RuleFor(x => x.Key)
                                              .NotEmpty()
                                              .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                                              .MaximumLength(216)
                                              .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH)
                                              .Must(ContainsInvalidChar)
                                              .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID)
                }
            );

            _serviceProvider = serviceProvider;
        }

        public override async Task<ValidationResult> ValidateAsync(ValidationContext<AddTemplates> context,
            CancellationToken cancellation = new CancellationToken())
        {
            var instance = context.InstanceToValidate;
            if (instance == null)
                return new ValidationResult();

            var baseValidationResult = await base.ValidateAsync(context, cancellation);
            if (!baseValidationResult.IsValid)
                return baseValidationResult;

            using (var serviceScope = _serviceProvider.CreateScope())
            {
                var serviceProvider = serviceScope.ServiceProvider;
                var addTemplatePayloadValidator = serviceProvider.GetService<IValidator<AddTemplatePayload>>();
                var addTemplateDetailsValidator = serviceProvider.GetService<IValidator<AddTemplateDetails>>();
                var addTemplateBindingValidator = serviceProvider.GetService<IValidator<AddTemplateBinding>>();

                var payloads = instance.Payloads;
                var bindings = instance.Bindings;
                if (payloads?.Any() == true)
                {
                    var payloadValidationResult = await ValidateTemplatePayload(payloads, addTemplatePayloadValidator, addTemplateDetailsValidator, cancellation);
                    if (payloadValidationResult != null)
                        return payloadValidationResult;
                    var bindingValidationResult = await ValidateTemplateBinding(bindings, addTemplateBindingValidator, cancellation);
                    if (bindingValidationResult != null)
                        return bindingValidationResult;
                }

                return new ValidationResult();
            }
        }

        private async Task<ValidationResult> ValidateTemplatePayload(IEnumerable<AddTemplatePayload> payloads, IValidator<AddTemplatePayload> addTemplatePayloadValidator, IValidator<AddTemplateDetails> addTemplateDetailsValidator, CancellationToken cancellation)
        {
            foreach (var payload in payloads)
            {
                var payloadValidationResult =
                    await addTemplatePayloadValidator.ValidateAsync(payload, cancellation);
                if (payloadValidationResult == null)
                    break;

                if (!payloadValidationResult.IsValid)
                    return payloadValidationResult;

                var details = payload.Details;
                if (details != null && details.Any())
                {
                    var detailValidationResult = await ValidateTemplateDetails(details, addTemplateDetailsValidator, cancellation);
                    if (detailValidationResult != null)
                        return detailValidationResult;
                }
            }
            return null;
        }

        private async Task<ValidationResult> ValidateTemplateDetails(IEnumerable<AddTemplateDetails> details, IValidator<AddTemplateDetails> addTemplateDetailsValidator, CancellationToken cancellation)
        {
            foreach (var detail in details)
            {
                var payloadDetailValidationResult =
                    await addTemplateDetailsValidator.ValidateAsync(detail, cancellation);
                if (!payloadDetailValidationResult.IsValid)
                    return payloadDetailValidationResult;
            }
            return null;
        }

        private async Task<ValidationResult> ValidateTemplateBinding(IEnumerable<AddTemplateBinding> bindings, IValidator<AddTemplateBinding> addTemplateBindingValidator, CancellationToken cancellation)
        {
            foreach (var binding in bindings)
            {
                var bindingValidationResult =
                    await addTemplateBindingValidator.ValidateAsync(binding, cancellation);

                if (bindingValidationResult == null)
                    break;

                if (!bindingValidationResult.IsValid)
                    return bindingValidationResult;
            }
            return null;
        }

        private bool ContainsInvalidChar(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return true;
            return !input.Contains(':') && !input.Contains(';') && !input.Contains(',');
        }
    }
}
