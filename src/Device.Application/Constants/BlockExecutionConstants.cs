namespace Device.Application.Constant
{
    public static class BlockExecutionConstants
    {
        public const string SYSTEM_TRIGGER_DATETIME = "system_trigger_datetime";
        public const string SYSTEM_SNAPSHOT_DATETIME = "system_snapshot_datetime";
        public static readonly string[] RESERVE_KEYWORDS = new[] { SYSTEM_TRIGGER_DATETIME, SYSTEM_SNAPSHOT_DATETIME };
    }
}
