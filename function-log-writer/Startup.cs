using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using AHI.Infrastructure.SharedKernel;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Function.Service;

[assembly: FunctionsStartup(typeof(AHI.Configuration.Function.Startup))]
namespace AHI.Configuration.Function
{
    public class Startup : FunctionsStartup
    {
        public Startup()
        {
            System.Diagnostics.Activity.DefaultIdFormat = System.Diagnostics.ActivityIdFormat.W3C;
        }

        public const string SERVICE_NAME = "device-function-log-writer";

        public override void Configure(IFunctionsHostBuilder builder)
        {
            // DI goes here           
            builder.Services.AddLoggingService();
            builder.Services.AddScoped<ILogService, FileSystemLogService>();
        }
    }
}