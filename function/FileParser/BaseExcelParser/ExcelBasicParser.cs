using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using NPOI.SS.UserModel;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.FileParser.Template.Abstraction;
using Function.Extension;
using AHI.Device.Function.Model.ImportModel.Validation;
using AHI.Device.Function.FileParser.ErrorTracking.Abstraction;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using AHI.Device.Function.FileParser.Model;

namespace AHI.Device.Function.FileParser.BaseExcelParser
{
    public delegate CellIndex NextStartCalculator(CellIndex startIndex, CellIndex endIndex, int skip);
    public delegate CellIndex DataStartCalculator(CellIndex startIndex);
    public delegate void ReadLineHandler(IDictionary<string, string> template, ISheet sheet, CellIndex index);
    public delegate bool ParseLineHandler(object model, ISheet sheet, CellIndex index, IDictionary<string, int> template, params string[] excludes);
    public class ExcelBasicParser
    {
        private readonly IDictionary<Placement, NextStartCalculator> _nextStartCalculators;
        private readonly IDictionary<Placement, DataStartCalculator> _dataStartCalculators;
        private readonly IDictionary<Placement, ReadLineHandler> _readDataHandlers;
        private readonly IDictionary<(Placement, ParseAction), ParseLineHandler> _parseDataHandlers;

        protected readonly IExcelTrackingService _errorService;
        public ExcelBasicParser(IExcelTrackingService errorService)
        {
            _errorService = errorService;

            _nextStartCalculators = new Dictionary<Placement, NextStartCalculator>
            {
                {Placement.ROW, NextObjectByRow},
                {Placement.COL, NextObjectByColumn},
            };
            _dataStartCalculators = new Dictionary<Placement, DataStartCalculator>
            {
                {Placement.ROW, SkipHeaderRow},
                {Placement.COL, SkipHeaderColumn}
            };
            _readDataHandlers = new Dictionary<Placement, ReadLineHandler>
            {
                {Placement.ROW, ReadTemplateRow},
                {Placement.COL, ReadTemplateColumn},
            };
            _parseDataHandlers = new Dictionary<(Placement, ParseAction), ParseLineHandler>
            {
                {(Placement.ROW, ParseAction.IMPORT), ParseImportRow},
                {(Placement.COL, ParseAction.IMPORT), ParseImportColumn}
            };
        }

        public NextStartCalculator GetNextStartCalculator(Placement placement) => _nextStartCalculators[placement];
        public DataStartCalculator GetDataStartCalculator(Placement placement) => _dataStartCalculators[placement];
        public ReadLineHandler GetReadTemplateHandler(Placement placement) => _readDataHandlers[placement];
        public ParseLineHandler GetImportParseDataHandler(Placement placement) => _parseDataHandlers[(placement, ParseAction.IMPORT)];

        public CellIndex NextObjectByRow(CellIndex startIndex, CellIndex endIndex, int skip)
        {
            return new CellIndex(startIndex.Row, endIndex.Col + skip);
        }

        public CellIndex NextObjectByColumn(CellIndex startIndex, CellIndex endIndex, int skip)
        {
            return new CellIndex(endIndex.Row + skip, startIndex.Col);
        }

        public CellIndex SkipHeaderRow(CellIndex startIndex)
        {
            return new CellIndex(startIndex.Row + 1, startIndex.Col);
        }

        public CellIndex SkipHeaderColumn(CellIndex startIndex)
        {
            return new CellIndex(startIndex.Row, startIndex.Col + 1);
        }

        public string ReadTemplateCell(ICell cell, bool is_template_cell = true)
        {
            if (cell != null && cell.CellType == CellType.String)
                return is_template_cell ? cell.ExtractTemplateString() : cell.StringCellValue;

            throw new InvalidOperationException($"Unexpected cell format. Row:{cell.RowIndex + 1}, Column:{cell.ColumnIndex + 1}.");
        }

        public void ParseCellImport(object model, string propertyName, ICell cell)
        {
            object inputValue = null;
            var inputType = typeof(int);
            try
            {
                var property = model.GetType().GetProperty(propertyName);

                if (property == null)
                {
                    return;
                }

                var value = cell is null ? null : cell.GetFormatedValue();
                inputValue = value;
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                inputType = type;
                try
                {
                    property.SetValue(model, value);
                }
                catch (ArgumentException)
                {
                    if (type == typeof(int))
                        value = CastToInteger(value, propertyName: propertyName);
                    if (type == typeof(float) || type == typeof(double))
                        value = CastToDouble(value, propertyName: propertyName);
                    property.SetValue(model, Convert.ChangeType(value, type));
                }
            }
            catch (TargetInvocationException e)
            {
                var errorData = e.InnerException?.Data["validationInfo"] as Dictionary<string, object>;
                _errorService.RegisterError((e.InnerException ?? e).Message, cell, errorData);
                throw;
            }
            catch (OverflowException e)
            {
                var validationInfo = new Dictionary<string, object>
                {
                    { "propertyName", propertyName },
                    { "propertyValue", inputValue }
                };
                HandleOverFlowException(e, validationInfo, inputValue, inputType, cell);
            }
            catch (FormatException)
            {
                var message = ValidationMessage.GENERAL_INVALID_VALUE;
                var validationInfo = new Dictionary<string, object>
                {
                    { "propertyName", propertyName },
                    { "propertyValue", inputValue }
                };
                _errorService.RegisterError(message, cell, validationInfo);
            }
            catch (InvalidCastException)
            {
                throw;
            }
            catch (Exception e)
            {
                var validationInfo = e.Data["validationInfo"] as Dictionary<string, object>;
                _errorService.RegisterError(e.Message, cell, validationInfo);
            }
        }

        private void HandleOverFlowException(OverflowException e, IDictionary<string, object> validationInfo, object inputValue, Type inputType, ICell cell)
        {
            string message = e.Message;
            switch (inputType)
            {
                case Type typeInt when typeInt == typeof(int):
                    if ((double)inputValue > int.MaxValue)
                    {
                        message = ValidationMessage.GREATER_THAN_MAX_VALUE;
                        validationInfo["comparisonValue"] = int.MaxValue;
                    }
                    else if ((double)inputValue < int.MinValue)
                    {
                        message = ValidationMessage.LESS_THAN_MIN_VALUE;
                        validationInfo["comparisonValue"] = int.MinValue;
                    }
                    break;
                case Type typeDouble when typeDouble == typeof(double):
                    if ((double)inputValue > double.MaxValue)
                    {
                        message = ValidationMessage.GREATER_THAN_MAX_VALUE;
                        validationInfo["comparisonValue"] = double.MaxValue;
                    }
                    else if ((double)inputValue < double.MinValue)
                    {
                        message = ValidationMessage.LESS_THAN_MIN_VALUE;
                        validationInfo["comparisonValue"] = double.MinValue;
                    }
                    break;
                default:
                    validationInfo = e.Data["validationInfo"] as Dictionary<string, object>;
                    break;
            }
            _errorService.RegisterError(message, cell, validationInfo);
        }

        public void ReadHeaderColumn(ICollection<string> headers, ISheet sheet, CellIndex index)
        {
            ICell cell;
            while ((cell = sheet.GetCell(index)) != null)
            {
                headers.Add(ReadTemplateCell(cell));
                index.Row++;
            }
            index.Row--;
        }

        public void ReadHeaderRow(ICollection<string> headers, ISheet sheet, CellIndex index)
        {
            ICell cell;
            while ((cell = sheet.GetCell(index)) != null)
            {
                headers.Add(ReadTemplateCell(cell));
                index.Col++;
            }
            index.Col--;
        }

        public void ReadHeaderRow(ICollection<string> headers, ISheet sheet, CustomHeaderRange customHeaderRange)
        {
            if (customHeaderRange.EndColumnIndex == -1)
            {
                ReadHeaderRow(headers, sheet, new CellIndex(customHeaderRange.RowIndex, customHeaderRange.StartColumnIndex));
            }
            else if (customHeaderRange.StartColumnIndex <= customHeaderRange.EndColumnIndex)
            {
                ICell cell;
                for (int columnIndex = customHeaderRange.StartColumnIndex; columnIndex <= customHeaderRange.EndColumnIndex; columnIndex++)
                {
                    cell = sheet.GetCell(new CellIndex(customHeaderRange.RowIndex, columnIndex));
                    if (cell != null)
                        headers.Add(ReadTemplateCell(cell));
                    else
                        headers.Add(null);
                }
            }
        }

        public void ReadTemplateColumn(IDictionary<string, string> template, ISheet sheet, CellIndex index)
        {
            ICell cell;
            while ((cell = sheet.GetCell(index)) != null)
            {
                var headerCell = sheet.GetCell(new CellIndex(index.Row, index.Col - 1));
                template.Add(ReadTemplateCell(headerCell, false), ReadTemplateCell(cell));
                index.Row++;
            }
            index.Row--;
        }

        public void ReadTemplateRow(IDictionary<string, string> template, ISheet sheet, CellIndex index)
        {
            ICell cell;
            while ((cell = sheet.GetCell(index)) != null)
            {
                var headerCell = sheet.GetCell(new CellIndex(index.Row - 1, index.Col));
                template.Add(ReadTemplateCell(headerCell, false), ReadTemplateCell(cell));
                index.Col++;
            }
            index.Col--;
        }

        public bool ParseImportColumn(object model, ISheet sheet, CellIndex index, IDictionary<string, int> template, params string[] excludes)
        {
            if (excludes == null || excludes.Length == 0)
            {
                excludes = new string[] { ExcelTemplateMetadata.END_HEADER };
            }
            var result = true;
            foreach (var name in template.Keys)
            {
                index.Row = template[name];
                if (!excludes.Contains(name))
                {
                    var cell = sheet.GetCell(index, true);
                    try
                    {
                        ParseCellImport(model, name, cell);
                    }
                    catch (Exception)
                    {
                        result &= false;
                    }
                }
            }
            return result;
        }

        public bool ParseImportRow(object model, ISheet sheet, CellIndex index, IDictionary<string, int> template, params string[] excludes)
        {
            if (excludes == null || excludes.Length == 0)
            {
                excludes = new string[] { ExcelTemplateMetadata.END_HEADER };
            }
            var result = true;

            foreach (var name in template.Keys)
            {
                index.Col = template[name];
                if (!excludes.Contains(name))
                {
                    var cell = sheet.GetCell(index, true);
                    try
                    {
                        ParseCellImport(model, name, cell);
                    }
                    catch (Exception e)
                    {
                        _errorService.RegisterError((e.InnerException ?? e).Message, cell);
                        result &= false;
                    }
                }
            }
            return result;
        }

        public bool ParseSheetName(object model, string sheetNameProperty, ISheet sheet)
        {
            try
            {
                var property = model.GetType().GetProperty(sheetNameProperty);
                var value = sheet.SheetName.Trim();
                property.SetValue(model, value);
                return true;
            }
            catch (TargetInvocationException e)
            {
                _errorService.RegisterError($"Sheet name invalid: {e.InnerException.Message}", sheet.SheetName);
                return false;
            }
        }

        private int CastToInteger(object input, bool strictMode = false, bool allowNotation = false, string propertyName = null)
        {
            propertyName ??= "{PropertyName}";
            return input switch
            {
                int intValue => intValue,
                double doubleValue => (int)(double)doubleValue,
                string stringValue when strictMode && StringRegex.NUMBER.IsMatch(input as string) => Convert.ToInt32(stringValue),
                string stringValue when allowNotation || StringRegex.DOUBLE.IsMatch(input as string) => int.TryParse(stringValue, out var result) ? result : throw new InvalidCastException($"Invalid {propertyName}: {stringValue}."),
                _ => throw new InvalidCastException($"Invalid {propertyName}: {input}."),
            };
        }

        private double CastToDouble(object input, bool allowNotation = false, string propertyName = null)
        {
            propertyName ??= "{PropertyName}";

            return input switch
            {
                double doubleValue => (double)doubleValue,
                int intValue => intValue,
                long longValue => longValue,
                string stringValue when (allowNotation || StringRegex.DOUBLE.IsMatch(input as string)) => double.TryParse(stringValue, out var value) ? value : double.MinValue,
                _ => throw new InvalidCastException($"Invalid {propertyName}: {input}."),
            };
        }
    }
}