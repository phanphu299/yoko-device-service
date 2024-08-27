using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.DataCompression.Model;


namespace AHI.Device.Function.Service
{
    public abstract class BaseDataProcessor<T> : IDataProcessor
    {
        protected abstract string DataType { get; }
        protected abstract Func<object, IDictionary<string, object>, T> ConvertValue { get; }
        protected virtual IEnumerable<CompressedMetric> CompressData(string timestamp, IEnumerable<MetricConfig> metricConfigs, IDictionary<string, object> payload, bool enableCompression = false)
        {
            return payload.Select(p => new CompressedMetric(p.Key, p.Value.ToString(), timestamp));
        }

        public IEnumerable<MetricSnapshot> Process(string timestamp, IEnumerable<DeviceMetric> deviceMetrics, IDictionary<string, object> payload, bool enableCompression = false)
        {
            deviceMetrics = deviceMetrics.Where(x => x.DataType == DataType);
            if (!deviceMetrics.Any())
            {
                return null;
            }
            var metricConfigs = deviceMetrics.Where(dm => dm.EnableDeadBand || dm.EnableSwingDoor)
            .Select(dm => new MetricConfig
            {
                MetricName = dm.MetricKey,
                EnableDeadBand = dm.EnableDeadBand,
                EnableSwingDoor = dm.EnableSwingDoor,
                IdleTimeout = dm.IdleTimeout,
                ExDevPlus = dm.ExDevPlus,
                ExDevMinus = dm.ExDevMinus,
                CompDevPlus = dm.CompDevPlus,
                CompDevMinus = dm.CompDevMinus
            });
            var compressedMetrics = CompressData(timestamp, metricConfigs, payload, enableCompression);

            var result = (from dm in deviceMetrics
                          join cm in compressedMetrics on dm.MetricKey equals cm.Name into gj
                          from sub in gj.DefaultIfEmpty()
                          select new MetricSnapshot
                          {
                              MetricKey = dm.MetricKey,
                              RetentionDays = dm.RetentionDays,
                              Value = ConvertValue(sub.Value, payload),
                              DeviceId = dm.DeviceId
                          }
                         ).ToArray();

            // update the dictionary
            // foreach (var item in result)
            // {
            //     if (item.Value != null)
            //     {
            //         payload[item.MetricKey] = item.Value;
            //     }
            //     else
            //     {
            //         item.Value = 0.0;
            //     }

            // }
            return result;
        }
    }
}