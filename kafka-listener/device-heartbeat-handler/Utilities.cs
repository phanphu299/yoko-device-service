using System.Linq;
using Device.Consumer.KraftShared.Models.Options;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Configuration;

namespace Device.Heartbeat.Handler
{
    public static class Utilities
    {
        public static RedisConfiguration MapToRedisConfiguration(RedisOptions option)
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
