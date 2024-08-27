using System.Linq;

namespace Device.Job.Constant
{
    public static class JobType
    {
        public const string QUERY_TIMESERIES = "query_timeseries";
        public const string QUERY_TIMESERIES_FULL_DATA = "query_timeseries_full_data";

        public static readonly string[] JOB_TYPES = { QUERY_TIMESERIES, QUERY_TIMESERIES_FULL_DATA };

        public static bool IsValidJobType(string jobType) => JOB_TYPES.Contains(jobType);
    }
}