using System.Linq;

namespace Device.Job.Constant
{
    public static class JobOutputType
    {
        public const string CSV = "csv";

        private readonly static string[] JOB_OUTPUT_TYPES = { CSV };

        public static bool IsValidOutputType(string outputType) => JOB_OUTPUT_TYPES.Contains(outputType);
    }
}