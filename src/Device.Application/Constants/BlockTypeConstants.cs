namespace Device.Application.Constant
{
    public static class BlockTypeConstants
    {
        public const string TYPE_BLOCK = "block";
        public const string TYPE_INPUT_CONNECTOR = "in_connector";
        public const string TYPE_OUTPUT_CONNECTOR = "out_connector";
    }
    public static class BindingDataTypeIdConstants
    {
        public const string TYPE_TEXT = "text";
        public const string TYPE_BOOLEAN = "bool";
        public const string TYPE_DOUBLE = "double";
        public const string TYPE_INTEGER = "int";
        public const string TYPE_DATETIME = "datetime";
        public const string TYPE_ASSET_ATTRIBUTE = "asset_attribute";
        public const string TYPE_ASSET_TABLE = "asset_table";

        public static readonly string[] NUMERIC_TYPES = new string[] { TYPE_DOUBLE, TYPE_INTEGER };
    }
    public static class BindingDataTypeNameConstants
    {
        public const string NAME_TEXT = "Text";
        public const string NAME_BOOLEAN = "Boolean";
        public const string NAME_DOUBLE = "Double";
        public const string NAME_INTEGER = "Integer";
        public const string NAME_DATETIME = "DateTime";
        public const string NAME_ASSET_ATTRIBUTE = "Asset Attribute";
        public const string NAME_ASSET_TABLE = "Asset Table";
    }
    public static class PathTypeConstants
    {
        public const string TYPE_BLOCK = "block";
        public const string TYPE_CATEGORY = "category";
    }

    public static class BindingTypeConstants
    {
        public const string INPUT = "input";
        public const string OUTPUT = "output";
        public const string INOUT = "inout";
    }
}
