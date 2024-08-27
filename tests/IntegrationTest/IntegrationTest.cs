using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using AHI.Infrastructure.IntegrationTest;
using Device.Api;
using AHI.Infrastructure.IntegrationTest.Models;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace AHI.IntegrationTest.Device
{
    public class IntegrationTest : BaseIntegrationTest, IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        public IntegrationTest(WebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTest");
                builder.ConfigureAppConfiguration((hostingContext, configBuilder) =>
                {
                    var env = hostingContext.HostingEnvironment;
                    configBuilder.AddJsonFile("appsettings.json", optional: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
                    configBuilder.AddEnvironmentVariables();
                });
                builder.ConfigureServices(serviceCollection =>
                {
                    // configure service here
                });
            });

            var logger = _factory.Services.GetService(typeof(ILogger<IntegrationTest>)) as ILogger<IntegrationTest>;
            SetLogger(logger);
        }


        protected override void SetLogger(ILogger<BaseIntegrationTest> logger)
        {
            _logger = logger;
        }

        [Fact]
        public async Task PostmanCollectionTesterAsync()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateParseHandling = DateParseHandling.DateTimeOffset,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                TypeNameHandling = TypeNameHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var httpClient = _factory.CreateClient();
            var postmanCollection = Path.Combine("AppData", "Test.postman_collection.json");
            var environmentPath = Path.Combine("AppData", "Localhost.postman_environment.json");
            var response = File.ReadAllText(postmanCollection);
            //var postmanEnvironment = File.ReadAllText(environmentPath);
            var postmanEnvironment = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(environmentPath));
            var environment = postmanEnvironment.ToObject<PostmanEnvironment>();
            var collection = JsonConvert.DeserializeObject<JObject>(response)["item"] as JArray;
            // await PreparingEnvironmentDataAsync(httpClient, environment);
            Assert.NotNull(environment);
            Assert.NotNull(collection);
            _logger.LogDebug("Starting TestThisSubCollectionAsync...;");
            await TestThisSubCollectionAsync(httpClient, environment, collection);
            _logger.LogDebug("Completed TestThisSubCollectionAsync!;");
        }
    }
}