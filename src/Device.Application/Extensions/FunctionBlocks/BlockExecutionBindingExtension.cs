using Device.Application.Constant;
using Device.Application.Model;

namespace Device.Application.Service
{
    public static class BlockExecutionBindingExtension
    {
        public static bool IsTextDataType(this IBlockExecutionBinding binding)
        {
            return binding.DataType == BindingDataTypeIdConstants.TYPE_TEXT;
        }

        public static bool IsBoolDataType(this IBlockExecutionBinding binding)
        {
            return binding.DataType == BindingDataTypeIdConstants.TYPE_BOOLEAN;
        }

        public static bool IsDoubleDataType(this IBlockExecutionBinding binding)
        {
            return binding.DataType == BindingDataTypeIdConstants.TYPE_DOUBLE;
        }

        public static bool IsIntDataType(this IBlockExecutionBinding binding)
        {
            return binding.DataType == BindingDataTypeIdConstants.TYPE_INTEGER;
        }

        public static bool IsDateTimeDataType(this IBlockExecutionBinding binding)
        {
            return binding.DataType == BindingDataTypeIdConstants.TYPE_DATETIME;
        }

        public static bool IsAssetAttributeDataType(this IBlockExecutionBinding binding)
        {
            return binding.DataType == BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE;
        }

        public static bool IsAssetTableDataType(this IBlockExecutionBinding binding)
        {
            return binding.DataType == BindingDataTypeIdConstants.TYPE_ASSET_TABLE;
        }
    }
}