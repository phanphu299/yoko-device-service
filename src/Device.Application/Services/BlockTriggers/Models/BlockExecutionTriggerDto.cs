namespace Device.Application.BlockFunction.Trigger.Model
{
    public class BlockExecutionTriggerDto
    {
        public string SubscriptionId { get; set; }
        public string ProjectId { get; set; }
        public bool OverrideTrigger { get; set; } = false;
    }
}