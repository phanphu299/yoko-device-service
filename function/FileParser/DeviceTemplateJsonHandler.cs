using System.Collections.Generic;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.FileParser.ErrorTracking.Abstraction;
using Function.Extension;
using FluentValidation;
using Newtonsoft.Json;
using AHI.Infrastructure.Import.Handler;
using JsonConstant = AHI.Infrastructure.SharedKernel.Extension.Constant;


namespace AHI.Device.Function.FileParser
{
    public class DeviceTemplateJsonHandler : JsonFileHandler<DeviceTemplate>
    {
        private readonly IJsonTrackingService _errorService;
        private readonly IValidator<DeviceTemplate> _validator;

        public DeviceTemplateJsonHandler(IJsonTrackingService errorService, IValidator<DeviceTemplate> validator)
        {
            _errorService = errorService;
            _validator = validator;
        }

        protected override IEnumerable<DeviceTemplate> Parse(JsonTextReader reader)
        {
            // Read object
            var template = reader.ReadSingleObject<DeviceTemplate>(
                JsonConstant.JsonSerializer,
                e => _errorService.RegisterError(ErrorMessage.FluentValidation.FORMAT_MISSING_FIELDS, ErrorType.PARSING));

            if (template is null)
                yield break;

            // Validate object
            var validation = _validator.Validate(template);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    _errorService.RegisterError(error.ErrorMessage, ErrorType.VALIDATING, error.FormattedMessagePlaceholderValues);
                }
                yield break;
            }

            yield return template;
        }
    }
}