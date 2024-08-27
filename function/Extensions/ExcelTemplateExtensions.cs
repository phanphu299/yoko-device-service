using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.FileParser.BaseExcelParser;
using System.Linq;

namespace Function.Extension
{
    public static class ExcelTemplateExtensions
    {
        private static readonly int _doublePrecision = 15;

        public static ICell GetCell(this ISheet sheet, CellIndex index, bool returnNullCell = false)
        {
            IRow row;
            if (sheet != null)
                if ((row = sheet.GetRow(index.Row)) != null)
                    return row.GetCell(index.Col, returnNullCell ? MissingCellPolicy.CREATE_NULL_AS_BLANK : MissingCellPolicy.RETURN_BLANK_AS_NULL);
            return null;
        }

        public static bool IsEmpty(this IRow row)
        {
            return row is null || row.Cells.Count < 1 || !row.Cells.Any(cell => cell.CellType != CellType.Blank);
        }
        
        public static object GetFormatedValue(this ICell cell)
        {
            if (cell is null || cell.CellType == CellType.Blank)
                return null;
            switch (cell.CellType)
            {
                case CellType.String:
                    {
                        return cell.StringCellValue.Trim();
                    }
                case CellType.Boolean:
                    {
                        return cell.BooleanCellValue;
                    }
                case CellType.Numeric:
                    {
                        if (DateUtil.IsCellDateFormatted(cell))
                            return cell.DateCellValue;

                        var value = cell.NumericCellValue;
                        if (value.IsInteger(_doublePrecision))
                        {
                            try
                            {
                                return Convert.ToInt32(value);
                            }
                            catch { }
                        }
                        return Math.Round(value, _doublePrecision);
                    }
                default:
                    return null;
            };
        }
        public static string ExtractTemplateString(this ICell cell) => cell.StringCellValue.Replace("<", "").Replace(">", "");
    }

    public static class StringConvertExtensions
    {
        static readonly IDictionary<Type, Func<string, object>> _stringConvertHandlers = new Dictionary<Type, Func<string, object>> {
            {typeof(string), x => x},
            {typeof(int), x => x.ToInt()},
            {typeof(bool), x => x.ToBoolean()},
            {typeof(Guid), x => x.ToGuid()},
            {typeof(Placement), x => x.ToPlacement()},
            {typeof(TemplateType), x => x.ToTemplateType()}
        };
        public static bool ToBoolean(this string value) => Convert.ToBoolean(value);
        public static int ToInt(this string value) => Convert.ToInt32(value);
        public static Guid ToGuid(this string value) => Guid.Parse(value);
        public static Placement ToPlacement(this string value) => (Placement)System.Enum.Parse<Placement>(value);
        public static TemplateType ToTemplateType(this string value) => (TemplateType)System.Enum.Parse<TemplateType>(value);
        public static T ToType<T>(this string value) => (T)_stringConvertHandlers[typeof(T)](value);
        public static object ToType(this string value, Type type) => _stringConvertHandlers[type](value);
    }
}