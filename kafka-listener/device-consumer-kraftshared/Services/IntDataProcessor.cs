using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using AHI.Infrastructure.DataCompression.Abstraction;
using AHI.Infrastructure.DataCompression.Model;

namespace Device.Consumer.KraftShared.Service
{
    public class IntDataProcessor : BaseDataProcessor<int?>
    {
        private readonly IDataCompressService _compressService;

        public IntDataProcessor(ILogger<IntDataProcessor> logger, IDataCompressService compressService)
        {
            _compressService = compressService;
        }

        protected override string DataType => "int";

        protected override Func<object, IDictionary<string, object>, int?> ConvertValue => (x, _) =>
         {
             if (Int32.TryParse(x.ToString(), out var result))
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
