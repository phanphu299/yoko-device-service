
namespace AHI.Device.Function.Constant
{
    public static class AttributeTypeConstants
    {
        public const string TYPE_STATIC = "static";
        public const string TYPE_DYNAMIC = "dynamic";
        public const string TYPE_RUNTIME = "runtime";
        public const string TYPE_ALIAS = "alias";
        public const string TYPE_INTEGRATION = "integration";
        public const string TYPE_COMMAND = "command";
        public static readonly string[] ATTRIBUTES_HAVE_DATA_TYPE = new string[] { TYPE_STATIC, TYPE_RUNTIME, TYPE_INTEGRATION, TYPE_DYNAMIC };
        public static readonly string[] ATTRIBUTES_HAVE_MARKUP = new string[] { TYPE_DYNAMIC, TYPE_COMMAND };
    }
}
