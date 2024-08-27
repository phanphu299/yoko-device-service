using System.Collections.Generic;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.FileParser.BaseExcelParser;
using AHI.Device.Function.FileParser.Template.Constant;
using AHI.Device.Function.FileParser.Template.Abstraction;

namespace AHI.Device.Function.FileParser.Template
{
    public class SimpleExcelTemplate : ExcelTemplate
    {
        public override TemplateType TemplateType => TemplateType.SIMPLE;
        private readonly ExcelDataParser _parser;

        public SimpleExcelTemplate(ExcelDataParser parser)
        {
            _parser = parser;
        }

        public override void LoadTemplate(XSSFWorkbook workbook, CellIndex cellIndex = null)
        {
            var sheet = workbook.GetSheet(TemplateSheetName.TEMPLATE_SHEET_NAME);
            var startIndex = new CellIndex(0, 0);
            IDictionary<string, string> template = new Dictionary<string, string>();
            _parser.ReadSingleTemplateRow(template, sheet, startIndex);
            PropertyTemplate = template;
        }

        public override bool ReadTemplateHeaderIndex(ISheet sheet, CellIndex cellIndex = null)
        {
            TemplateHeaderIndex.Clear();

            var headers = new List<string>();
            var startIndex = cellIndex ?? new CellIndex(0, 0);
            var currentIndex = startIndex.Clone();
            _parser.ReadHeaderRow(headers, sheet, currentIndex);

            if (headers.Count != PropertyTemplate.Count)
                return false;

            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                if (PropertyTemplate.TryGetValue(header, out var propName))
                    TemplateHeaderIndex[propName] = startIndex.Col + i;
            }
            if (TemplateHeaderIndex.Count != PropertyTemplate.Count)
                return false;

            TemplateHeaderIndex[ExcelTemplateMetadata.END_HEADER] = currentIndex.Col;
            return true;
        }
    }
}
