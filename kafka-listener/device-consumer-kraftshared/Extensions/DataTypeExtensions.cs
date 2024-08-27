using System.Linq;
using Device.Consumer.KraftShared.Constant;

namespace Device.Consumer.KraftShared.Extensions
{
    public static class DataTypeExtensions
    {
        private static readonly string[] METRIC_SERIES_NUMERIC_TYPES = new string[] { DataTypeConstants.TYPE_BOOLEAN, DataTypeConstants.TYPE_DOUBLE, DataTypeConstants.TYPE_INTEGER };
        private static readonly string[] METRIC_SERIES_TEXT_TYPES = new string[] { DataTypeConstants.TYPE_TEXT, DataTypeConstants.TYPE_DATETIME };

        /// <summary>
        /// Return the list of all data type using for Asset's attributes.
        /// </summary>
        /// <seealso cref="IsDataTypeForAttribute">Please consider using this function for checking if Data Type is valid (or not).</seealso>
        public static readonly string[] ATTRIBUTE_DATA_TYPES = new string[] { DataTypeConstants.TYPE_BOOLEAN, DataTypeConstants.TYPE_DOUBLE, DataTypeConstants.TYPE_INTEGER, DataTypeConstants.TYPE_TEXT, DataTypeConstants.TYPE_DATETIME };

        /// <summary>
        /// Return the list of all data type using for Asset Template's attributes.
        /// </summary>
        /// <seealso cref="IsDataTypeForTemplateAttribute">Please consider using this function for checking if Data Type is valid (or not).</seealso>
        public static readonly string[] TEMPLATE_ATTRIBUTE_DATA_TYPES = new string[] { DataTypeConstants.TYPE_BOOLEAN, DataTypeConstants.TYPE_DOUBLE, DataTypeConstants.TYPE_INTEGER, DataTypeConstants.TYPE_TEXT };


        public static bool IsNumericTypeSeries(string dataType)
        {
            return CompareDataType(METRIC_SERIES_NUMERIC_TYPES, dataType);
        }
        public static bool IsTextTypeSeries(string dataType)
        {
            return CompareDataType(METRIC_SERIES_TEXT_TYPES, dataType);
        }
        public static bool IsDataTypeForAttribute(string dataType)
        {
            return CompareDataType(ATTRIBUTE_DATA_TYPES, dataType);
        }
        public static bool IsDataTypeForTemplateAttribute(string dataType)
        {
            return CompareDataType(TEMPLATE_ATTRIBUTE_DATA_TYPES, dataType);
        }


        private static bool CompareDataType(string[] listDataTypes, string dataType)
        {
            return listDataTypes.Any(x => string.Equals(dataType, x, System.StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
