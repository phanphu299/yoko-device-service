using System.Threading;
using AHI.Infrastructure.Bus.ServiceBus.Extension;
using AHI.Infrastructure.Interceptor.Extension;
using Confluent.Kafka;
using Device.Consumer.KraftShared.Extensions;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models;
using Device.Consumer.KraftShared.Models.Options;
using Device.Consumer.KraftShared.Service;
using Device.Consumer.KraftShared.Service.Abstraction;
using Device.Consumer.KraftShared.Services.HealthCheck;
using Device.Consumer.KraftShared.Abstraction;
using Device.Consumer.SnapshotSyncHandler.Processor;
using Device.Consumer.SnapshotSyncHandler.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using StackExchange.Redis.Extensions.System.Text.Json;

namespace Device.Consumer.SnapshotSyncHandler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(3000, 3000);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .WriteTo.Debug(LogEventLevel.Debug)
                .WriteTo.Console(LogEventLevel.Debug)
                .CreateBootstrapLogger();

            Log.Information("Starting up");
            var builder = Host.CreateApplicationBuilder(args);
            var services = builder.Services;
            var cfgService1 = builder.Configuration;
            builder.Services.AddLogging();
            builder.Services.AddSerilog();
            services.AddSingleton<IProducer<Null, byte[]>>(sp =>
            {

                var cfgService = sp.GetRequiredService<IConfiguration>();
                var kafkaOption = cfgService.GetSection("Kafka").Get<KafkaOption>();
                var bootstrapServers = kafkaOption.BootstrapServers;
                var config = new ProducerConfig
                {
                    BootstrapServers = bootstrapServers,
                    Acks = kafkaOption?.Producer?.AckMode ?? Acks.Leader,
                    LingerMs = kafkaOption?.Producer?.Linger ?? 100,
                };
                return new ProducerBuilder<Null, byte[]>(config).Build();
            });
            services.Configure<KafkaOption>(cfgService1.GetSection("Kafka"));
            services.Configure<RedisOptions>(cfgService1.GetSection("Redis"));
            services.Configure<BatchProcessingOptions>(cfgService1.GetSection("BatchProcessing"));
            services.AddDeviceConsumerShared(cfgService1);
            services.AddSingleton<IPublisher, KafkaPublisher>();
            services.AddServiceBus();
            services.AddMemoryCache();
            services.AddInterceptor();
            var redisOptions = cfgService1.GetSection("Redis").Get<RedisOptions>();
            var redisConfiguration = Utilities.MapToRedisConfiguration(redisOptions);
            services.AddStackExchangeRedisExtensions<SystemTextJsonSerializer>(redisConfiguration);
            services.Configure<HostOptions>(options =>
            {
                //This option allow multiple Hosted Service running concurrently
                options.ServicesStartConcurrently = true; //<--- new .NET 8 feature
                options.ServicesStopConcurrently = true; //<--- new .NET 8 feature
            });

            // lock
            services.AddSingleton<ILockFactory, SemaphoreSlimLockFactory>();
            // add health check
            services.AddHealthChecks().AddCheck<MemoryHealthCheck>("memory_hc");
            services.AddHealthChecks().AddCheck<KafkaHealthCheck>("kafka_consumer");
            services.AddSingleton<KafkaHealthCheckService>();
            services.AddHostedService<HealthProbeService>(); // add healthcheck
            // Common
            services.AddSingleton(typeof(ChannelProvider<,>));
            services.AddSingleton(typeof(KafkaPartitionsHandler<,>));
            // Ingestion
            services.AddHostedService<SyncSnapshotBackgroundService>();
            // TrackingHeartBeat
            var app = builder.Build();
            app.Run();
        }
    }

}
