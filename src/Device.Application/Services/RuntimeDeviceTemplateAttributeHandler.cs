using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Device.Application.Constant;
using Device.ApplicationExtension.Extension;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;

namespace Device.Application.Service
{
    public class RuntimeDeviceTemplateAttributeHandler
    {
        private readonly ILoggerAdapter<RuntimeAssetTemplateAttributeHandler> _logger;
        private readonly IDynamicResolver _dynamicResolver;
        public RuntimeDeviceTemplateAttributeHandler(
            IDynamicResolver dynamicResolver,
            ILoggerAdapter<RuntimeAssetTemplateAttributeHandler> logger)
        {
            _logger = logger;
            _dynamicResolver = dynamicResolver;
        }

        public Task<(bool, string)> ValidateExpressionAsync(Guid templateId, string expressionValidate, string dataType, IEnumerable<(Guid DetailId, string MetricKey, string DataTypeName)> metrics)
        {
            // need to make sure the expression is valid and compile
            // *** TODO: NOW VALUE WILL NOT IN VALUE COLUMN ==> now alway true
            if (string.IsNullOrWhiteSpace(expressionValidate))
                return Task.FromResult<(bool, string)>((false, null));

            // if (dataType == DataTypeConstants.TYPE_TEXT)
            // {
            //     // for text, no need to validate
            //     return (true, expressionValidate);
            // }
            //get metric in expresstion : {a}*2+{b} => {}
            ICollection<Guid> detailIds = new List<Guid>();
            Match m = Regex.Match(expressionValidate, RegexConstants.PATTERN_EXPRESSION_KEY, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(10));
            //get metric name in expression
            while (m.Success)
            {
                string idProperty = m.Value.Replace("${", "").Replace("}$", "").Trim();
                Guid.TryParse(idProperty, out Guid detailId);
                if (!detailIds.Contains(detailId) && detailId != Guid.Empty)
                    detailIds.Add(detailId);
                m = m.NextMatch();
            }
            // var expressionRelationAttributes = await _deviceUnitOfWork.TemplateDetailRepository.AsQueryable().AsNoTracking().Where(x => x.Payload.TemplateId == templateId && detailIds.Contains(x.DetailId) && x.Enabled).Select(x => new { MetricKey = x.Key, DetailId = x.DetailId, DataTypeName = x.DataType }).ToListAsync();
            // if (optionalParametters != null && optionalParametters.Any())
            // {
            //     // https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/5032
            //     expressionRelationAttributes.AddRange(optionalParametters.Where(x => !expressionRelationAttributes.Any(m => m.DetailId == x.detailId)).Select(x => new { MetricKey = x.metricKey, DetailId = x.detailId, DataTypeName = x.dataType }));
            // }
            var validMetrics = (from key in detailIds
                                join expression in metrics on key equals expression.DetailId
                                select key);
            if (detailIds.Count > 0 && detailIds.Count != validMetrics.Count())
            {
                // some key is not found in the database -> error
                return Task.FromResult<(bool, string)>((false, null));
            }
            var dictionary = new Dictionary<string, object>();
            expressionValidate = BuildExpression(expressionValidate, metrics, dictionary);
            if (dataType == DataTypeConstants.TYPE_TEXT)
            {
                //if expression contain special character, we need to escape it one more time
                if (!metrics.Any(x => expressionValidate.Contains($"request[\"{x.MetricKey.ToString()}\"]")))
                {
                    expressionValidate = expressionValidate.ToJson();
                }
            }
            if (!expressionValidate.Contains("return "))
            {
                expressionValidate = $"return {expressionValidate}";
            }
            try
            {
                _logger.LogTrace(expressionValidate);
                var value = _dynamicResolver.ResolveInstance("return true;", expressionValidate).OnApply(dictionary);
                if (!string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    var result = value.ParseResultWithDataType(dataType);
                    return Task.FromResult<(bool, string)>((result, expressionValidate));
                }
            }
            catch (System.Exception exc)
            {
                _logger.LogError(exc, exc.Message);
            }
            return Task.FromResult<(bool, string)>((false, null));
        }

        private string BuildExpression(string expressionValidate, IEnumerable<(Guid DetailId, string MetricKey, string DataTypeName)> metrics, IDictionary<string, object> dictionary)
        {
            foreach (var element in metrics)
            {
                object value = null;
                switch (element.DataTypeName.ToLower())
                {
                    case DataTypeConstants.TYPE_DOUBLE:
                        expressionValidate = expressionValidate.Replace($"${{{element.DetailId}}}$", $"Convert.ToDouble(request[{element.MetricKey.ToJson()}])");
                        value = 1.0;
                        break;
                    case DataTypeConstants.TYPE_INTEGER:
                        expressionValidate = expressionValidate.Replace($"${{{element.DetailId}}}$", $"Convert.ToInt32(request[{element.MetricKey.ToJson()}])");
                        value = 1;
                        break;
                    case DataTypeConstants.TYPE_BOOLEAN:
                        expressionValidate = expressionValidate.Replace($"${{{element.DetailId}}}$", $"Convert.ToBoolean(request[{element.MetricKey.ToJson()}])");
                        value = true;
                        break;
                    case DataTypeConstants.TYPE_TEXT:
                        expressionValidate = expressionValidate.Replace($"${{{element.DetailId}}}$", $"request[{element.MetricKey.ToJson()}].ToString()");
                        value = "default";
                        break;
                }
                dictionary[element.MetricKey] = value;
            }
            return expressionValidate;
        }
    }
}
