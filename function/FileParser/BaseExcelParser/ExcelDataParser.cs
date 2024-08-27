using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using Function.Extension;
using FluentValidation;
using AHI.Device.Function.FileParser.ErrorTracking.Abstraction;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.Model;
using AHI.Device.Function.FileParser.Template.Abstraction;

namespace AHI.Device.Function.FileParser.BaseExcelParser
{
    public class ExcelDataParser : ExcelBasicParser
    {
        private readonly IDictionary<Type, IValidator> _validators;

        public ExcelDataParser(IDictionary<Type, IValidator> validators, IExcelTrackingService errorService) :
            base(errorService)
        {
            _validators = validators;
        }
        public bool ParseSheetNameProperty<T>(T model, string sheetNameProperty, ISheet sheet) where T : TrackModel
        {
            return ParseSheetName(model, sheetNameProperty, sheet);
        }

        public void ReadSingleTemplateColumn(IDictionary<string, string> template, ISheet sheet, CellIndex index)
        {
            var currentIndex = SkipHeaderColumn(index);
            ReadTemplateColumn(template, sheet, currentIndex);
            currentIndex.CopyTo(index);
        }

        public void ReadSingleTemplateRow(IDictionary<string, string> template, ISheet sheet, CellIndex index)
        {
            var currentIndex = SkipHeaderRow(index);
            ReadTemplateRow(template, sheet, currentIndex);
            currentIndex.CopyTo(index);
        }

        public bool ParseSingleColumn<T>(T model, ISheet sheet, CellIndex index, IDictionary<string, int> template, params string[] excludes) where T : TrackModel
        {
            var currentIndex = SkipHeaderColumn(index);
            var result = ParseImportColumn(model, sheet, currentIndex, template, excludes);

            if (result)
            {
                _errorService.Track(model, index.Col);
                result = ValidateModel(model, _validators[typeof(T)]);
            }
            currentIndex.CopyTo(index);
            return result;
        }

        public bool ParseMultiRow<T>(ICollection<T> models, ISheet sheet, CellIndex index, IDictionary<string, int> template) where T : TrackModel
        {
            var dataStart = SkipHeaderRow(index);
            var result = true;
            while (!sheet.GetRow(dataStart.Row).IsEmpty())
            {
                var model = Activator.CreateInstance<T>();
                var currentIndex = dataStart.Clone();
                var success = ParseImportRow(model, sheet, currentIndex, template, ExcelTemplateMetadata.END_HEADER);

                if (success)
                {
                    _errorService.Track(model, currentIndex.Row);
                    success = ValidateModel(model, _validators[typeof(T)]);
                    if (success)
                        models.Add(model);
                }

                result &= success;
                currentIndex.CopyTo(index);
                dataStart = NextObjectByColumn(dataStart, currentIndex, 1);
            }

            return result;
        }

        public bool ParseMultiRow(Type type, ICollection<object> models, ISheet sheet, CellIndex index, IDictionary<string, int> template)
        {
            var dataStart = SkipHeaderRow(index);
            var result = true;
            while (!sheet.GetRow(dataStart.Row).IsEmpty())
            {
                var model = Activator.CreateInstance(type) as TrackModel;
                var currentIndex = dataStart.Clone();
                var success = ParseImportRow(model, sheet, currentIndex, template, ExcelTemplateMetadata.END_HEADER);

                if (success)
                {
                    _errorService.Track(model, currentIndex.Row);
                    success = ValidateModel(model, _validators[type]);
                    if (success)
                        models.Add(model);
                }

                result &= success;
                currentIndex.CopyTo(index);
                dataStart = NextObjectByColumn(dataStart, currentIndex, 1);
            }

            return result;
        }

        private bool ValidateModel(TrackModel model, IValidator validator)
        {
            var validation = validator.Validate(model);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    _errorService.RegisterError(error.ErrorMessage, model,
                                                model.ErrorProperty(error.PropertyName), ErrorType.VALIDATING, error.FormattedMessagePlaceholderValues);
                }
            }
            return validation.IsValid;
        }
    }
}