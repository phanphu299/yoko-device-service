using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.Template;
using AHI.Device.Function.FileParser.Template.Abstraction;
using AHI.Device.Function.FileParser.Model;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.Template.Constant;
using System.Linq;

namespace AHI.Device.Function.FileParser.BaseExcelParser
{
    public class ComplexParser<T> : ISheetParser<T> where T : ImportModel
    {
        private ComplexExcelTemplate _template;
        private NextStartCalculator _toChildStartCalculator;

        protected override void SetParser(ExcelDataParser parser)
        {
            base.SetParser(parser);
            _toChildStartCalculator = _parser.GetNextStartCalculator(Placement.COL);
        }

        protected override void SetTemplate(ExcelTemplate template)
        {
            _template = template as ComplexExcelTemplate;
        }

        public override IEnumerable<T> ParseSheet(ISheet sheet, bool forceGetResult = false, CellIndex cellIndex = null)
        {
            var startIndex = cellIndex ?? new CellIndex(0, 0);
            if (!_template.ReadTemplateHeaderIndex(sheet, startIndex) || !CheckCustomHeaderTemplate(sheet))
            {
                _errorService.RegisterError(ErrorMessage.FluentValidation.FORMAT_MISSING_FIELDS, ErrorType.PARSING);
                yield break;
            }

            _errorService.SetTemplateInfo(sheet.SheetName, Placement.COL, _template.TemplateHeaderIndex, _template.SheetNameProperty);

            T result = Activator.CreateInstance<T>();
            var success = _parser.ParseSheetNameProperty(result, _template.SheetNameProperty, sheet);

            var currentIndex = startIndex.Clone();
            success &= _parser.ParseSingleColumn(result, sheet, currentIndex, _template.TemplateHeaderIndex, ExcelTemplateMetadata.END_HEADER, _template.SheetNameProperty);

            _errorService.SetTemplateInfo(sheet.SheetName, Placement.ROW, _template.ChildTemplateHeaderIndex);
            var childStartIndex = _toChildStartCalculator.Invoke(startIndex, currentIndex, _template.ObjectChildGap);
            currentIndex = childStartIndex.Clone();

            IList<object> childResult = new List<object>();
            success &= _parser.ParseMultiRow((result as ComplexModel).ChildType, childResult, sheet, currentIndex, _template.ChildTemplateHeaderIndex);
            (result as ComplexModel).ChildProperty = childResult;

            if (!success)
                yield break;
            yield return result;
        }

        public bool CheckCustomHeaderTemplate(ISheet importSheet)
        {
            if (_customHeaderNeedToCheck != null)
            {
                var templateSheet = _template.GetTemplateWorkbook().GetSheet(TemplateSheetName.TEMPLATE_SHEET_NAME);
                var importHeaders = new List<string>();
                var templateHeaders = new List<string>();
                foreach (var customHeaderRange in _customHeaderNeedToCheck)
                {
                    _parser.ReadHeaderRow(importHeaders, importSheet, customHeaderRange);
                    _parser.ReadHeaderRow(templateHeaders, templateSheet, customHeaderRange);
                }
                return templateHeaders.SequenceEqual(importHeaders, StringComparer.InvariantCultureIgnoreCase);
            }
            return true;
        }
    }
}