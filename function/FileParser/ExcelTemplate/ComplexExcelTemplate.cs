using System.Collections.Generic;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.FileParser.BaseExcelParser;
using AHI.Device.Function.FileParser.Template.Constant;
using AHI.Device.Function.FileParser.Template.Abstraction;

namespace AHI.Device.Function.FileParser.Template
{
    public class ComplexExcelTemplate : ExcelTemplate
    {
        public override TemplateType TemplateType => TemplateType.COMPLEX;
        private IDictionary<string, string> ChildPropertyTemplate { get; set; }
        public IDictionary<string, int> ChildTemplateHeaderIndex = new Dictionary<string, int>();
        private readonly ExcelDataParser _parser;
        private readonly NextStartCalculator _toChildStartCalculator;
        
        public ComplexExcelTemplate(ExcelDataParser parser)
        {
            _parser = parser;
            _toChildStartCalculator = parser.GetNextStartCalculator(Placement.COL);
        }

        public override void LoadTemplate(XSSFWorkbook workbook, CellIndex cellIndex = null)
        {
            LoadMetadata(workbook);

            var sheet = workbook.GetSheet(TemplateSheetName.TEMPLATE_SHEET_NAME);
            var startIndex = cellIndex != null ? cellIndex : new CellIndex(0, 0);
            var currentIndex = startIndex.Clone();

            IDictionary<string, string> template = new Dictionary<string, string>();
            var nextStartIndex = _toChildStartCalculator.Invoke(startIndex, currentIndex, ObjectChildGap);

            template = new Dictionary<string, string>();
            _parser.ReadSingleTemplateRow(template, sheet, nextStartIndex);
            PropertyTemplate = template;

            ChildPropertyTemplate = template;
        }

        public override bool ReadTemplateHeaderIndex(ISheet sheet, CellIndex cellIndex = null)
        {
            TemplateHeaderIndex.Clear();
            ChildTemplateHeaderIndex.Clear();

            var headers = new List<string>();
            var startIndex = cellIndex ?? new CellIndex(0, 0);
            var currentIndex = startIndex.Clone();

            TemplateHeaderIndex[ExcelTemplateMetadata.END_HEADER] = currentIndex.Row;
            TemplateHeaderIndex[SheetNameProperty] = currentIndex.Row;

            headers.Clear();
            var nextStartIndex = _toChildStartCalculator.Invoke(startIndex, currentIndex, ObjectChildGap);
            nextStartIndex.CopyTo(currentIndex);
            _parser.ReadHeaderRow(headers, sheet, currentIndex);

            if (headers.Count != PropertyTemplate.Count)
                return false;

            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                if (ChildPropertyTemplate.TryGetValue(header, out var propName))
                    ChildTemplateHeaderIndex[propName] = nextStartIndex.Col + i;
            }
            if (ChildTemplateHeaderIndex.Count != ChildPropertyTemplate.Count)
                return false;

            ChildTemplateHeaderIndex[ExcelTemplateMetadata.END_HEADER] = currentIndex.Col;
            return true;
        }
    }
}