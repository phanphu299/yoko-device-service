using Device.Consumer.KraftShared.Extensions;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models.Options;
using Device.Consumer.KraftShared.Repositories.Abstraction.ReadOnly;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Pipelines.Sockets.Unofficial.Arenas;
using Serilog;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.System.Text.Json;

namespace redis_memcache_migration
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            ThreadPool.SetMinThreads(3000, 3000);
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddEnvironmentVariables();
            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddLogging();
            builder.Services.AddSerilog();
            builder.Services.AddDeviceConsumerShared(builder.Configuration);
            builder.Services.Configure<Device.Consumer.KraftShared.Models.Options.RedisOptions>(builder.Configuration.GetSection("Redis"));
            builder.Services.AddMemoryCache();
            builder.Services.Configure<BatchProcessingOptions>(builder.Configuration.GetSection("BatchProcessing"));
            var redisOptions = builder.Configuration.GetSection("Redis").Get<Device.Consumer.KraftShared.Models.Options.RedisOptions>();
            var redisConfiguration = Utilities.MapToRedisConfiguration(redisOptions);
            builder.Services.AddStackExchangeRedisExtensions<SystemTextJsonSerializer>(redisConfiguration);
            var app = builder.Build();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            Log.Information("Starting MqttListener application");
            var projectIds = await GetProjectIds(builder.Configuration.GetSection("ProjectUri").Get<string>());
            Log.Information($"projectIds: {JsonConvert.SerializeObject(projectIds)}");
            if (projectIds == null || !projectIds.Any())
            {
                Log.Fatal("ProjectIds is missing");
                return;
            }
            await RunMigrationAsync(builder.Services, projectIds);
            Log.Information("Migration completed");
            app.Run();
        }

        private static async Task<List<string>> GetProjectIds(string projectUri)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync($"{projectUri}&migrated=true&type=asset").Result;

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON into the C# object using Newtonsoft.Json
                var projects = JsonConvert.DeserializeObject<List<ProjectDto>>(responseBody);

                return projects != null ? projects.Select(x => x.Id).ToList() : new List<string>();
            }
        }

        static async Task RunMigrationAsync(IServiceCollection service, IEnumerable<string> projectIds)
        {
            //var projectIds = new string[] {
            //    //"f3a50740-4223-491b-82b6-fa29cfc27ead",//perf-006
            //    "c9d1c876-0345-4924-9c2f-54d7a5207d90",//perf-009
            //    "09ff2a0c-4dfc-4031-9103-58c51de1c471",//perf-010
            //    "8387cc8e-1fb6-45db-83f2-cf61dbcfbe3d",//perf-008
            //    "005bbdfc-2340-4146-b08d-dbdeaee86d75",//perf-001
            //    "724501ee-cc22-47aa-839b-0cfb572ce561"//perf-011
            //};
            var provider = service.BuildServiceProvider();
            var dr = provider.GetService<IReadOnlyDeviceRepository>();
            
            foreach (var projectId in projectIds)
            {
                await dr.LoadAllNecessaryResourcesAsync(projectId);
            }
        }
    }
    internal static class Utilities
    {
        public static RedisConfiguration MapToRedisConfiguration(Device.Consumer.KraftShared.Models.Options.RedisOptions option)
        {
            var config = new RedisConfiguration
            {
                AbortOnConnectFail = option.AbortOnConnectFail,
                AllowAdmin = option.AllowAdmin,
                ConnectTimeout = option.ConnectTimeout,
                Database = option.DefaultDatabase,
                ConnectRetry = option.ConnectRetry,
                Password = option.Password,
                ServiceName = option.ServiceName,
                Ssl = option.Ssl,
                SyncTimeout = option.SyncTimeout,


            };
            config.Hosts = option.Host.Split(",").Select(h => new RedisHost { Host = h, Port = option.Port }).ToArray();
            return config;
        }
        public static ConfigurationOptions MapToConfigurationOptions(RedisOptions option)
        {
            var config = new ConfigurationOptions
            {
                AbortOnConnectFail = option.AbortOnConnectFail,
                AllowAdmin = option.AllowAdmin,
                AsyncTimeout = option.AsyncTimeout,
                ClientName = option.ClientName,
                ConfigCheckSeconds = option.ConfigCheckSeconds,
                ConnectTimeout = option.ConnectTimeout,
                DefaultDatabase = option.DefaultDatabase,
                ConnectRetry = option.ConnectRetry,
                KeepAlive = option.KeepAlive,
                Password = option.Password,
                ServiceName = option.ServiceName,
                SslHost = option.SslHost,
                Ssl = option.Ssl,
                SyncTimeout = option.SyncTimeout,
            };
            foreach (var hostport in option.HostPorts)
                config.EndPoints.Add(hostport);
            return config;
        }

    }
}
