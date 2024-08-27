using StackExchange.Redis;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Device.Consumer.KraftShared.Services.Abstractions
{
    public interface ICacheManager
    {
        Task<bool> AddAsync<T>(string key, T value, TimeSpan? expiresIn = null, When when = When.Always, CommandFlags flag = CommandFlags.None, HashSet<string>? tags = null);
        Task<T?> GetAsync<T>(string key, CommandFlags flag = CommandFlags.None);

        Task<T?> GetAsync<T>(string key, DateTimeOffset expiresAt, CommandFlags flag = CommandFlags.None);

        Task<T?> GetAsync<T>(string key, TimeSpan expiresIn, CommandFlags flag = CommandFlags.None);
        Task<T?> HashGetAsync<T>(string hashKey, string key, CommandFlags flag = CommandFlags.None);

        Task<IDictionary<string, T?>> HashGetAsync<T>(string hashKey, string[] keys, CommandFlags flag = CommandFlags.None);
        Task<bool> HashSetAsync<T>(string hashKey, string key, T value, bool nx = false, CommandFlags flag = CommandFlags.None);
        Task HashSetAsync<T>(string hashKey, IDictionary<string, T> values, CommandFlags flag = CommandFlags.None);
        Task<bool> SetAddAsync<T>(string key, T item, CommandFlags flag = CommandFlags.None);
        Task StoreAsync(string key, object value, TimeSpan? ttl = null, int db = 11);
        Task<string> StringGetAsync(string key, int db = 11);
        Task StringSetAsync(string key, object value, TimeSpan? ttl = null, int db = 11);
    }
}
