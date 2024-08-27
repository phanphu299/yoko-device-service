using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using AHI.Device.Function.Service.Abstraction;

namespace AHI.Device.Function.Timer
{
    public class LogCleaner
    {
        private readonly ILogService _logService;

        public LogCleaner(ILogService logService)
        {
            _logService = logService;
        }

        [FunctionName("LogCleaner")]
        public void RunAsync([TimerTrigger("0 0 1 * * *")] TimerInfo timer, ILogger log)
        {
            _logService.DeleteExpiredFiles();
        }
    }
}