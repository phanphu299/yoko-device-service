using NPOI.XSSF.UserModel;
using Function.Extension;
using AHI.Device.Function.FileParser.BaseExcelParser;
using AHI.Device.Function.FileParser.Template.Constant;

namespace AHI.Device.Function.FileParser.Template.Abstraction
{
    public abstract class ExcelTemplateMetadata
    {
        public const string END_HEADER = "<NULL>";
        public string SheetNameProperty { get; set; } = null;
        public int ObjectChildGap { get; set; } = 1;

        public void LoadMetadata(XSSFWorkbook workbook)
        {
            var sheet = workbook.GetSheet(TemplateSheetName.METADATA_SHEET_NAME);
            var index = new CellIndex(-1, 1);
            foreach (var property in typeof(ExcelTemplateMetadata).GetProperties())
            {
                index.Row = MetadataIndexMapping.GetIndex(property.Name);
                var value = sheet.GetCell(index).ExtractTemplateString();
                property.SetValue(this, value.ToType(property.PropertyType));
            }
        }
    }
}