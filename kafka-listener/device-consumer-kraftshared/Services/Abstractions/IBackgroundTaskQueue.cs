using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Enums;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models.MetricModel;

namespace Device.Consumer.KraftShared.Services.Abstractions
{
    public interface IBackgroundTaskQueue
    {
        void ExecuteRedisHashSetFireAndForgetAsync<T>(string redisKey, string hashField, T data);
        void ExecuteRedisHashSetDictionaryFireAndForgetAsync<T>(string redisKey, IDictionary<string, T> data);
        void ExecuteStoreDeviceSnapshotBackgroundAsync(string tenantId, string subscriptionId, string projectId, IEnumerable<MetricValue> snapshotMetricsInput);
        void ExecuteStoreAttributeRuntimeSnapshotBackgroundAsync(string tenantId, string subscriptionId, string projectId, IEnumerable<RuntimeValueObject> runtimeValues);
    }
}
