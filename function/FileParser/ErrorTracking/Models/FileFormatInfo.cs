using System.Collections.Generic;
using AHI.Device.Function.FileParser.Constant;

namespace AHI.Device.Function.FileParser.ErrorTracking.Model
{
    public abstract class FileFormatInfo
    {
    }

    public class ExcelSheetInfo : FileFormatInfo
    {
        public string SheetName { get; set; }
        public Placement Placement { get; set; }
        public IDictionary<string, int> Template { get; set; }
        public ExcelSheetInfo(string sheetName, Placement placement, IDictionary<string, int> template)
        {
            SheetName = sheetName;
            Placement = placement;
            Template = template;
        }
    }
}