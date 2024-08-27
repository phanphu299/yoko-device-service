using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Models.Options;
using Device.Consumer.KraftShared.Services.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Device.Consumer.KraftShared.Services
{
    public class CacheManager : ICacheManager
    {
        private readonly ConnectionMultiplexer _sharedMultiplexer;
        private readonly ConfigurationOptions _redisOptions;
        public CacheManager(IOptions<RedisOptions> configuration)
        {
            _redisOptions = Map(configuration.Value);
            _sharedMultiplexer = ConnectionMultiplexer.Connect(_redisOptions);
        }


        public async Task<bool> AddAsync<T>(string key, T value, When when = When.Always, CommandFlags flag = CommandFlags.None, System.Collections.Generic.HashSet<string> tags = null)
        {
            var dbInstance = _sharedMultiplexer.GetDatabase();
            await StringSetAsync(key, value, null);
            return true;
        }

        public async Task<bool> AddAsync<T>(string key, T value, TimeSpan? expiresIn = null, When when = When.Always, CommandFlags flag = CommandFlags.None, HashSet<string> tags = null)
        {
            var dbInstance = _sharedMultiplexer.GetDatabase();
            await StringSetAsync(key, value, expiresIn);
            return true;
        }

        public async Task<T?> GetAsync<T>(string key, int db = 11)
        {
            var dbInstance = _sharedMultiplexer.GetDatabase(db);
            var json = await dbInstance.StringGetAsync(key);
            if (string.IsNullOrEmpty(json))
                return default(T);
            return System.Text.Json.JsonSerializer.Deserialize<T?>(json);
        }

        public async Task<T?> GetAsync<T>(string key, CommandFlags flag = CommandFlags.None)
        {
            var dbInstance = _sharedMultiplexer.GetDatabase();
            var json = await dbInstance.StringGetAsync(key);
            if (string.IsNullOrEmpty(json))
                return default(T);
            return System.Text.Json.JsonSerializer.Deserialize<T?>(json);
        }

        public async Task<T?> GetAsync<T>(string key, DateTimeOffset expiresAt, CommandFlags flag = CommandFlags.None)
        {
            throw new NotImplementedException();
        }

        public async Task<T?> GetAsync<T>(string key, TimeSpan expiresIn, CommandFlags flag = CommandFlags.None)
        {
            throw new NotImplementedException();
        }

        public async Task<T> HashGetAsync<T>(string hashKey, string key, CommandFlags flag = CommandFlags.None)
        {
            var dbInstance = _sharedMultiplexer.GetDatabase();
            var json = await dbInstance.HashGetAsync(key, hashKey, flag);
            if (string.IsNullOrEmpty(json))
                return default(T);
            return JsonSerializer.Deserialize<T>(json);
        }

        public async Task<IDictionary<string, T?>> HashGetAsync<T>(string hashKey, string[] keys, CommandFlags flag = CommandFlags.None)
        {
            throw new NotImplementedException();
        }


        public async Task<bool> HashSetAsync<T>(string hashKey, string key, T value, bool nx = false, CommandFlags flag = CommandFlags.None)
        {
            var dbInstance = _sharedMultiplexer.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            if (string.IsNullOrEmpty(json))
                return false;
            await dbInstance.HashSetAsync(key, new HashEntry[] { new HashEntry(hashKey, json) });
            return true;
        }

        public async Task HashSetAsync<T>(string hashKey, IDictionary<string, T> values, CommandFlags flag = CommandFlags.None)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SetAddAsync<T>(string key, T item, CommandFlags flag = CommandFlags.None)
        {
            var dbInstance = _sharedMultiplexer.GetDatabase();
            var json = JsonSerializer.Serialize(item);
            if (string.IsNullOrEmpty(json))
                return false;
            await dbInstance.SetAddAsync(key, json);
            return true;
        }

        public async Task StoreAsync(string key, object value, TimeSpan? ttl = null, int db = 11)
        {
            var dbInstance = _sharedMultiplexer.GetDatabase(db);
            await StringSetAsync(key, value, ttl, db);
        }

        public async Task<string> StringGetAsync(string key, int db = 11)
        {
            var dbInstance = _sharedMultiplexer.GetDatabase(db);
            var value = await dbInstance.StringGetAsync(key);
            return (string)value;
        }

        public async Task StringSetAsync(string key, object value, TimeSpan? ttl = null, int db = 11)
        {
            var dbInstance = _sharedMultiplexer.GetDatabase(db);
            if (value is string || value is String)
                if (ttl != null)
                    await dbInstance.StringSetAsync(key, (string)value, ttl);
                else
                    await dbInstance.StringSetAsync(key, (string)value);
            else
                if (ttl != null)
                await dbInstance.StringSetAsync(key, JsonSerializer.Serialize(value), ttl);
            else
                await dbInstance.StringSetAsync(key, JsonSerializer.Serialize(value));
        }

        private ConfigurationOptions Map(RedisOptions redisOptions)
        {
            var config = new ConfigurationOptions
            {
                AbortOnConnectFail = redisOptions.AbortOnConnectFail,
                AllowAdmin = redisOptions.AllowAdmin,
                AsyncTimeout = redisOptions.AsyncTimeout,
                ClientName = redisOptions.ClientName,
                ConfigCheckSeconds = redisOptions.ConfigCheckSeconds,
                ConnectTimeout = redisOptions.ConnectTimeout,
                DefaultDatabase = redisOptions.DefaultDatabase,
                ConnectRetry = redisOptions.ConnectRetry,
                KeepAlive = redisOptions.KeepAlive,
                Password = redisOptions.Password,
                ServiceName = redisOptions.ServiceName,
                SslHost = redisOptions.SslHost,
                Ssl = redisOptions.Ssl,
                SyncTimeout = redisOptions.SyncTimeout,
            };
            foreach (var hostport in redisOptions.HostPorts)
                config.EndPoints.Add(hostport);
            return config;
        }
    }
}
