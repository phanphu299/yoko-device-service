
namespace AHI.Device.Function.Constant
{
    public static class RegexConstants
    {
        public const string PATTERN_EXPRESSION_KEY = @"([\$][\{][(\w)?(\d)?(\-)?(\ )?]*[\}][\$])";
        public const string DEVICE_TEMPLATE_EXPRESSION_PATTERN = @"\$\{ (.*?) \}\$";
        public const string EXPRESSION_REFER_OPEN = "${ ";
        public const string EXPRESSION_REFER_CLOSE = " }$";
        public const string IOT_HUB_DEVICE_ID = @"^[a-zA-Z0-9\-\.%_*?!(),:=@\$']{0,127}[a-zA-Z0-9\-%_*?!(),:=@\$']{1}$";
        public const string PATTERN_PROJECT_ID = @"^[a-fA-F0-9-]{36}";
        public const string PATTERN_TELEMETRY_TOPIC = @"^[a-fA-F0-9-]{36}\/devices\/[^\/#+$*]+\/telemetry$";
        public const string PATTERN_COMMAND_TOPIC = @"^[a-fA-F0-9-]{36}\/devices\/[^\/#+$*]+\/commands$";
        public const string PATTERN_REPLACE_EXPRESSION = @"(?<!return)\s";
    }
}
