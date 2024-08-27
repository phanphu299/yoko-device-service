using System.Threading;
using AHI.Infrastructure.Bus.ServiceBus.Extension;
using AHI.Infrastructure.Interceptor.Extension;
using Confluent.Kafka;
using Device.Consumer.KraftShared.Abstraction;
using Device.Consumer.Kraft.Processor;
using Device.Consumer.Kraft.Services;
using Device.Consumer.KraftShared.Extensions;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models;
using Device.Consumer.KraftShared.Models.Options;
using Device.Consumer.KraftShared.Service.Abstraction;
using Device.Consumer.KraftShared.Services;
using Device.Consumer.KraftShared.Services.Abstractions;
using Device.Consumer.KraftShared.Services.HealthCheck;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using StackExchange.Redis.Extensions.System.Text.Json;
using AHI.Infrastructure.MultiTenancy.Http.Handler;
using System;
using Device.Consumer.KraftShared.Constant;

namespace Device.Consumer.Kraft
{
    public class Program
    {
        const string SERVICE_NAME = "device-consumer-kafka";

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
            services.AddSingleton<IProducer<string, byte[]>>(sp =>
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
                return new ProducerBuilder<string, byte[]>(config).Build();
            });
            services.Configure<RedisOptions>(cfgService1.GetSection("Redis"));
            services.Configure<KafkaOption>(cfgService1.GetSection("Kafka"));
            services.Configure<BatchProcessingOptions>(cfgService1.GetSection("BatchProcessing"));
            services.Configure<GeneralOptions>(cfgService1.GetSection("General"));
            services.AddSingleton<IBackgroundTaskQueue, CommonBackgroundTaskQueue>();
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

            services.AddHealthChecks().AddCheck<MemoryHealthCheck>("memory_hc");
            services.AddHealthChecks().AddCheck<KafkaHealthCheck>("kafka_consumer");
            services.AddSingleton<KafkaHealthCheckService>();
            services.AddHostedService<HealthProbeService>(); // add healthcheck
            // Common
            services.AddSingleton(typeof(ChannelProvider<,>));
            services.AddSingleton(typeof(KafkaPartitionsHandler<,>));
            // lock
            services.AddSingleton<ILockFactory, SemaphoreSlimLockFactory>();
            // Ingestion
            services.AddSingleton<IngestionProcessor>();
            services.AddHostedService<IngestionBackgroundService>();
            builder.Services.AddHttpClient(ClientNameConstant.FUNCTION_BLOCK, (service, client) =>
                        {
                            var configuration = service.GetRequiredService<IConfiguration>();
                            client.BaseAddress = new Uri(configuration["Api:FunctionBlock"]);
                        }).AddHttpMessageHandler<ClientCrendetialAuthentication>();
            var app = builder.Build();
            app.Run();
        }
    }

}
