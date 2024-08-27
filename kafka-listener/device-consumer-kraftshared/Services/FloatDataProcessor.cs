using System;
using System.Collections.Generic;
using AHI.Infrastructure.DataCompression.Abstraction;
using AHI.Infrastructure.DataCompression.Model;
using Microsoft.Extensions.Logging;

namespace Device.Consumer.KraftShared.Service
{
    public class FloatDataProcessor : BaseDataProcessor<double?>
    {
        private readonly IDataCompressService _compressService;
        public FloatDataProcessor(ILogger<FloatDataProcessor> logger, IDataCompressService compressService)
        {
            _compressService = compressService;
        }

        protected override string DataType => "double";


        protected override Func<object, IDictionary<string, object>, double?> ConvertValue => (x, _) =>
         {
             if (x == null || string.IsNullOrEmpty(x.ToString()))
             {
                 return null;
             }
             if (double.TryParse(x.ToString(), out var result))
             {
                 return result;
             }
             return null;
         };

        protected override IEnumerable<CompressedMetric> CompressData(string timestamp, IEnumerable<MetricConfig> metricConfigs, IDictionary<string, object> payload, bool enableCompression = false)
        {
            if (enableCompression)
            {
                return _compressService.Compress(timestamp, metricConfigs, payload);
            }
            else
            {
                return base.CompressData(timestamp, metricConfigs, payload);
            }
        }
    }
}
