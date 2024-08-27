using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Device.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            System.Diagnostics.Activity.DefaultIdFormat = System.Diagnostics.ActivityIdFormat.W3C;
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging(logging =>
                {
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
                        .AddEnvironmentVariables()
                        .Build();

                        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                            .ReadFrom.Configuration(configuration);

                        OverrideMinimumLevel(configuration, loggerConfiguration);

                        loggerConfiguration
                            .Enrich.FromLogContext()
                            .Enrich.WithThreadId()
                            .Enrich.WithThreadName()
                            .WriteTo.Console(new JsonFormatter());

                        var logger = loggerConfiguration.CreateLogger();
                        logging.ClearProviders();
                        logging.AddSerilog(logger);

                        logger.Information("Start user service!");
                });

        private static void OverrideMinimumLevel(IConfigurationRoot configuration, LoggerConfiguration loggerConfiguration)
        {
            List<KeyValuePair<string, string>> keyValuePairs = ConverSerilogSettings(configuration);

            foreach (var kv in keyValuePairs)
            {
                string key = kv.Key;
                string value = kv.Value;

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                    continue;

                if (key == "Logging__LogLevel__Default" || key == "Serilog__MinimumLevel__Default")
                {
                    if (value == LogEventLevel.Verbose.ToString())
                        loggerConfiguration.MinimumLevel.Verbose();
                    else if (value == LogEventLevel.Debug.ToString())
                        loggerConfiguration.MinimumLevel.Debug();
                    else if (value == LogEventLevel.Information.ToString())
                        loggerConfiguration.MinimumLevel.Information();
                    else if (value == LogEventLevel.Warning.ToString())
                        loggerConfiguration.MinimumLevel.Warning();
                    else if (value == LogEventLevel.Error.ToString())
                        loggerConfiguration.MinimumLevel.Error();
                    else if (value == LogEventLevel.Fatal.ToString())
                        loggerConfiguration.MinimumLevel.Fatal();
                    else
                        loggerConfiguration.MinimumLevel.Information();
                }
                else if (key.StartsWith("Logging__LogLevel__"))
                {
                    LogEventLevel logEventLevel = GetLogEventLevel(value);
                    loggerConfiguration.MinimumLevel.Override(key.Replace("Logging__LogLevel__", ""), logEventLevel);
                }
                else if (key.StartsWith("Serilog__MinimumLevel__Override__"))
                {
                    LogEventLevel logEventLevel = GetLogEventLevel(value);
                    loggerConfiguration.MinimumLevel.Override(key.Replace("Serilog__MinimumLevel__Override__", ""), logEventLevel);
                }
            }
        }

        private static LogEventLevel GetLogEventLevel(string value)
        {
            LogEventLevel logEventLevel = LogEventLevel.Information;

            if (value == LogEventLevel.Debug.ToString())
                logEventLevel = LogEventLevel.Debug;
            else if (value == LogEventLevel.Verbose.ToString())
                logEventLevel = LogEventLevel.Verbose;
            else if (value == LogEventLevel.Warning.ToString())
                logEventLevel = LogEventLevel.Warning;
            else if (value == LogEventLevel.Error.ToString())
                logEventLevel = LogEventLevel.Error;
            else if (value == LogEventLevel.Fatal.ToString())
                logEventLevel = LogEventLevel.Fatal;
            return logEventLevel;
        }

        private static List<KeyValuePair<string, string>> ConverSerilogSettings(IConfigurationRoot configurationRoot)
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, string>> keyValues = configurationRoot.AsEnumerable().ToList();

            foreach (KeyValuePair<string, string> kv in keyValues)
            {
                if (kv.Key.StartsWith("Logging__LogLevel__") || kv.Key.StartsWith("Serilog__MinimumLevel__"))
                {
                    result.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
                }
            }

            return result;
        }
    }
}
