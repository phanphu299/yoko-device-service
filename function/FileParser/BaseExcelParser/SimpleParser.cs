using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.FileParser.Model;
using AHI.Device.Function.FileParser.Template;
using AHI.Device.Function.FileParser.Template.Abstraction;

namespace AHI.Device.Function.FileParser.BaseExcelParser
{
    public class SimpleParser<T> : ISheetParser<T> where T : ImportModel
    {
        private SimpleExcelTemplate _template;
        public override ExcelTemplate ParserTemplate => _template;

        protected override void SetTemplate(ExcelTemplate template)
        {
            _template = template as SimpleExcelTemplate;
        }

        public override IEnumerable<T> ParseSheet(ISheet sheet, bool forceGetResult = false, CellIndex cellIndex = null)
        {
            var startIndex = cellIndex ?? new CellIndex(0, 0);
            if (!_template.ReadTemplateHeaderIndex(sheet, startIndex))
            {
                if (!forceGetResult)
                    _errorService.RegisterError(ErrorMessage.FluentValidation.FORMAT_MISSING_FIELDS, ErrorType.PARSING);
                return Array.Empty<T>();
            }

            _errorService.SetTemplateInfo(sheet.SheetName, Placement.ROW, _template.TemplateHeaderIndex);

            ICollection<T> result = new List<T>();
            var success = _parser.ParseMultiRow(result, sheet, startIndex, _template.TemplateHeaderIndex);
            if (success || forceGetResult)
                return result;
            else
                return Array.Empty<T>();
        }
    }
}