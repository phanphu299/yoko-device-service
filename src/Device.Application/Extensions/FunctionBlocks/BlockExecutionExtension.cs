using System.Collections.Generic;
using Device.Application.Constant;

namespace Device.Application.Service
{
    public static class BlockExecutionExtension
    {
        private readonly static IList<string> _runningStatus = new List<string>() { BlockExecutionStatusConstants.RUNNING, BlockExecutionStatusConstants.RUNNING_OBSOLETE };
        private readonly static IList<string> _stoppedStatus = new List<string>() { BlockExecutionStatusConstants.STOPPED, BlockExecutionStatusConstants.STOPPED_ERROR };

        public static bool IsRunning(this string status)
        {
            return _runningStatus.Contains(status);
        }

        public static bool IsStopped(this string status)
        {
            return _stoppedStatus.Contains(status);
        }
    }
}