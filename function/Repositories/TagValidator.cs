using System.Collections.Generic;
using System.Data;
using System.Linq;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.BaseExcelParser;
using AHI.Device.Function.Model;
using AHI.Infrastructure.Service.Tag.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;

namespace AHI.Infrastructure.Repository
{
    public class TagValidator
    {
        private const int TAG_MAX_LENGTH = 435;
        private const int KEY_VALUE_MAX_LENGTH = 216;
        private readonly IImportTrackingService _errorService;

        public TagValidator(IImportTrackingService errorService)
        {
            _errorService = errorService;
        }

        public bool ValidateTags<T>(string inputTags, T model, CellIndex cellIndex = null) where T : Device.Function.FileParser.Model.TrackModel
        {
            var result = true;
            var tagList = inputTags.Split(TagConstants.TAG_IMPORT_EXPORT_SEPARATOR)
                                  .Where(tag => !string.IsNullOrWhiteSpace(tag));
            var invalidTags = tagList.Where(tag => tag.Split(":").Count() != 2);
            if (invalidTags.Any())
            {
                foreach (var tag in invalidTags)
                {
                    _errorService.RegisterError(ValidationMessage.GENERAL_INVALID_VALUE, model, "Tags", cellIndex, ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyName", "Tag" },
                        { "propertyValue", tag }
                    });
                }

                result = false;
            }

            var tags = tagList.Where(tag => !invalidTags.Contains(tag))
                                .Select(tag => new UpsertTag
                                {
                                    Key = tag.Split(":")[0].Trim(),
                                    Value = tag.Split(":")[1].Trim()
                                });

            var invalidCharTags = tags.Where(tag => tag.Key.Contains(";") ||
                                                    tag.Value.Contains(";"));
            if (invalidCharTags.Any())
            {
                foreach (var tag in invalidCharTags)
                {
                    _errorService.RegisterError(ValidationMessage.GENERAL_INVALID_VALUE, model, "Tags", cellIndex, ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyName", "Tag" },
                        { "propertyValue", $"{tag.Key} : {tag.Value}" },
                    });
                }

                result = false;
            }

            var emptyKeyValues = tags.Where(tag => string.IsNullOrWhiteSpace(tag.Key) || string.IsNullOrWhiteSpace(tag.Value));
            if (emptyKeyValues.Any())
            {
                foreach (var tag in emptyKeyValues)
                {
                    _errorService.RegisterError(ValidationMessage.GENERAL_INVALID_VALUE, model, "Tags", cellIndex, ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyName", "Tag" },
                        { "propertyValue", $"{tag.Key} : {tag.Value}" },
                    });
                }

                result = false;
            }

            var exceedLengthTags = tags.Where(tag => tag.Key.Length > KEY_VALUE_MAX_LENGTH && tag.Value.Length > KEY_VALUE_MAX_LENGTH);
            if (exceedLengthTags.Any())
            {
                foreach (var tag in exceedLengthTags)
                {
                    _errorService.RegisterError(ValidationMessage.TAG_MAX_LENGTH, model, "Tags", cellIndex, ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyValue", $"{tag.Key} : {tag.Value}" },
                        { "comparisonValue", TAG_MAX_LENGTH }
                    });
                }

                result = false;
            }

            var exceedLengthKeys = tags.Where(tag => tag.Key.Length > KEY_VALUE_MAX_LENGTH &&
                                                     tag.Value.Length <= KEY_VALUE_MAX_LENGTH);
            if (exceedLengthKeys.Any())
            {
                foreach (var tag in exceedLengthKeys)
                {
                    _errorService.RegisterError(ValidationMessage.TAG_KEY_MAX_LENGTH, model, "Tags", cellIndex, ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyValue", $"{tag.Key} : {tag.Value}" },
                        { "comparisonValue", KEY_VALUE_MAX_LENGTH }
                    });
                }

                result = false;
            }

            var exceedLengthValues = tags.Where(tag => tag.Value.Length > KEY_VALUE_MAX_LENGTH &&
                                                       tag.Key.Length <= KEY_VALUE_MAX_LENGTH);
            if (exceedLengthValues.Any())
            {
                foreach (var tag in exceedLengthValues)
                {
                    _errorService.RegisterError(ValidationMessage.TAG_VALUE_MAX_LENGTH, model, "Tags", cellIndex, ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyValue", $"{tag.Key} : {tag.Value}" },
                        { "comparisonValue", KEY_VALUE_MAX_LENGTH }
                    });
                }

                result = false;
            }

            return result;
        }

        public bool ValidateTags<T>(IEnumerable<ImportExportTagDto> tags, T model, CellIndex cellIndex = null) where T : Device.Function.FileParser.Model.TrackModel
        {
            var result = true;
            var invalidCharTags = tags.Where(tag => (!string.IsNullOrWhiteSpace(tag.Key) &&
                                                     (tag.Key.Contains(";") ||
                                                     tag.Key.Contains(",") ||
                                                     tag.Key.Contains(":"))) ||
                                                     (!string.IsNullOrWhiteSpace(tag.Value) &&
                                                     (tag.Value.Contains(";") ||
                                                     tag.Value.Contains(",") ||
                                                     tag.Value.Contains(":"))));
            if (invalidCharTags.Any())
            {
                foreach (var tag in invalidCharTags)
                {
                    _errorService.RegisterError(ValidationMessage.GENERAL_INVALID_VALUE, model, "Tags", cellIndex, ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyName", "Tag" },
                        { "propertyValue", $"{tag.Key} : {tag.Value}" },
                    });
                }

                result = false;
            }

            var emptyKeys = tags.Where(tag => string.IsNullOrWhiteSpace(tag.Key) &&
                                                    !string.IsNullOrWhiteSpace(tag.Value));
            if (emptyKeys.Any())
            {
                foreach (var tag in emptyKeys)
                {
                    _errorService.RegisterError(ValidationMessage.GENERAL_INVALID_VALUE, model, "Tags", ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyName", "Tag" },
                        { "propertyValue", $"{tag.Key} : {tag.Value}" }
                    });
                }

                result = false;
            }

            var emptyValues = tags.Where(tag => string.IsNullOrWhiteSpace(tag.Value) &&
                                                     !string.IsNullOrWhiteSpace(tag.Key));
            if (emptyValues.Any())
            {
                foreach (var tag in emptyValues)
                {
                    _errorService.RegisterError(ValidationMessage.GENERAL_INVALID_VALUE, model, "Tags", ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyName", "Tag" },
                        { "propertyValue", $"{tag.Key} : {tag.Value}" }
                    });
                }

                result = false;
            }

            var exceedLengthTags = tags.Where(tag => tag.Key?.Length > KEY_VALUE_MAX_LENGTH &&
                                                          tag.Value?.Length > KEY_VALUE_MAX_LENGTH);
            if (exceedLengthTags.Any())
            {
                foreach (var tag in exceedLengthTags)
                {
                    _errorService.RegisterError(ValidationMessage.TAG_MAX_LENGTH, model, "Tags", ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyValue", $"{tag.Key} : {tag.Value}" },
                        { "comparisonValue", TAG_MAX_LENGTH }
                    });
                }

                result = false;
            }

            var exceedLengthKeys = tags.Where(tag => tag.Key?.Length > KEY_VALUE_MAX_LENGTH &&
                                                          tag.Value?.Length <= KEY_VALUE_MAX_LENGTH);
            if (exceedLengthKeys.Any())
            {
                foreach (var tag in exceedLengthKeys)
                {
                    _errorService.RegisterError(ValidationMessage.TAG_KEY_MAX_LENGTH, model, "Tags", ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyValue", $"{tag.Key} : {tag.Value}" },
                        { "comparisonValue", KEY_VALUE_MAX_LENGTH }
                    });
                }

                result = false;
            }

            var exceedLengthValues = tags.Where(tag => tag.Value?.Length > KEY_VALUE_MAX_LENGTH &&
                                                            tag.Key?.Length <= KEY_VALUE_MAX_LENGTH);
            if (exceedLengthValues.Any())
            {
                foreach (var tag in exceedLengthValues)
                {
                    _errorService.RegisterError(ValidationMessage.TAG_VALUE_MAX_LENGTH, model, "Tags", ErrorType.VALIDATING, new Dictionary<string, object>
                    {
                        { "propertyValue", $"{tag.Key} : {tag.Value}" },
                        { "comparisonValue", KEY_VALUE_MAX_LENGTH }
                    });
                }

                result = false;
            }

            return result;
        }
    }
}