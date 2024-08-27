using Device.Application.Constant;
using Device.Application.Model;

namespace Device.Application.Service
{
    public static class BlockExecutionSnapshotExtension
    {
        public static bool IsTriggerTypeAssetAttribute(this BlockExecutionSnapshot snapshot)
        {
            return snapshot.TriggerType == BlockFunctionTriggerConstants.TYPE_ASSET_ATTRIBUTE_EVENT;
        }

        public static bool IsTriggerTypeScheduler(this BlockExecutionSnapshot snapshot)
        {
            return snapshot.TriggerType == BlockFunctionTriggerConstants.TYPE_SCHEDULER;
        }
    }
}