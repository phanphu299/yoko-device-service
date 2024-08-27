using System.IO;
using System.Collections.Generic;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.BaseExcelParser;
using AHI.Device.Function.FileParser.Template.Constant;
using Function.Extension;

namespace AHI.Device.Function.FileParser.Template.Abstraction
{
    public abstract class ExcelTemplate : ExcelTemplateMetadata
    {
        public static readonly string EXTENSION = ".xlsx";
        public abstract TemplateType TemplateType { get; }
        public IDictionary<string, string> PropertyTemplate { get; set; }
        public IDictionary<string, int> TemplateHeaderIndex { get; } = new Dictionary<string, int>();
        public abstract void LoadTemplate(XSSFWorkbook workbook, CellIndex cellIndex = null);
        public abstract bool ReadTemplateHeaderIndex(ISheet sheet, CellIndex cellIndex = null);
        private static XSSFWorkbook _templateWorkbook { get; set; }
        public static ExcelTemplate GetTemplate(string templateName, IParserContext context, CellIndex cellIndex = null)
        {
            var templatePath = context.GetTemplatePath($"{templateName}{EXTENSION}");
            using (var stream = new FileStream(templatePath, FileMode.Open))
            {
                stream.Position = 0;
                var workbook = new XSSFWorkbook(stream);

                var sheet = workbook.GetSheet(TemplateSheetName.METADATA_SHEET_NAME);
                var typeIndex = new CellIndex(MetadataIndexMapping.GetIndex(nameof(TemplateType)), 1);
                var cell = sheet.GetCell(typeIndex);
                var type = cell.ExtractTemplateString().ToType<TemplateType>();

                var template = context.GetTemplate(type);
                template.LoadTemplate(workbook, cellIndex);
                _templateWorkbook = workbook;
                return template;
            }
        }

        public XSSFWorkbook GetTemplateWorkbook()
        {
            return _templateWorkbook;
        }
    }
}