namespace Device.Application.Constant
{
    public static class BlockExecutionMessageConstants
    {
        public const string VALIDATION_FAIL = "Validation fail";
        public const string PUBLISH_FAIL = "Publish fail";
        public const string EXECUTION_FAIL = "Execution fail";
        public const string REFRESH_MARKUP_FAIL = "Refresh markup fail";
        public const string REFRESH_TEMPLATE_FAIL = "Refresh template fail";
        public const string CONSTRUCT_TRIGGER_FAIL = "Construct trigger fail";
        public const string CONSTRUCT_MAPPING_FAIL = "Construct mapping fail";
        public const string TRIGGER_ASSET_NOT_FOUND = "Trigger asset not found";
        public const string TRIGGER_ATTRIBUTE_NOT_FOUND = "Trigger attribute not found";
        public const string BLOCK_EXECUTION_ERROR = "Block Execution {0} has error: {1}"; // 0:blockExecutionId, 1:rootCauseMessage
    }
}