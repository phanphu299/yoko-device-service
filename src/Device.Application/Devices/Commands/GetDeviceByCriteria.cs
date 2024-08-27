using Device.Application.Constants;
using Device.Application.Device.Command.Model;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;
using AHI.Infrastructure.Service.Dapper.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using AHI.Infrastructure.Service.Dapper.Helpers;
using AHI.Infrastructure.SharedKernel.Models;
using System.Text.RegularExpressions;
using System;
using AHI.Infrastructure.Service.Model;
using AHI.Infrastructure.SharedKernel.Extension;
using System.Linq;

namespace Device.Application.Device.Command
{
    public class GetDeviceByCriteria : BaseCriteria, IRequest<BaseSearchResponse<GetDeviceDto>>
    {

        public GetDeviceByCriteria()
        {
            Sorts = DefaultSearchConstants.DEFAULT_SORT;
            Fields = new[] { "Id", "Name", "TemplateId", "Template", "Status", "UpdatedUtc", "RetentionDays", "DeviceSnaphot", "EnableHealthCheck", "SignalQualityCode" };
        }

        private static readonly Dictionary<string, string> _keyMap = new Dictionary<string, string>()
        {
            ["id"] = "d.id",
            ["id.tolower()"] = "lower(d.id)",
            ["name"] = "d.name",
            ["name.tolower()"] = "lower(d.name)",
            ["status"] = "d.status",
            ["status.tolower()"] = "lower(d.status)",
            ["createdutc"] = "d.created_utc",
            ["updatedutc"] = "d.updated_utc",
            ["retentiondays"] = "d.retention_days",
            ["enablehealthcheck"] = "d.enable_health_check",
            ["signalqualitycode"] = "d.signal_quality_code",
            ["template.id"] = "dt.id",
            ["template.id.tolower()"] = "lower(dt.id)",
            ["template.name"] = "dt.name",
            ["template.name.tolower()"] = "lower(dt.name)",
            ["template.createdby"] = "dt.created_by",
            ["template.createdutc"] = "dt.created_utc",
            ["template.updatedutc"] = "dt.updated_utc",
            ["template.totalmetric"] = "dt.total_metric",
            ["devicesnaphot.status"] = "dms.status",
            ["devicesnaphot.timestamp"] = "dms._ts",
            ["devicesnaphot.status.tolower()"] = "lower(dms.status)"
        };

        public GetDeviceQueryCriteria ToQueryCriteria()
        {
            var queryCriteria = new GetDeviceQueryCriteria
            {
                Filter = Filter != null ? JsonConvert.DeserializeObject<JObject>(Filter) : null,
                PageIndex = PageIndex,
                PageSize = PageSize,
                Sorts = Sorts
            };
            if (!string.IsNullOrEmpty(Filter))
            {
                queryCriteria.Filter = DynamicCriteriaHelper.ProcessDapperQueryFilter(Filter, queryCriteria.Filter, queryCriteria, ConvertSearchFilter);
            }
            if (!string.IsNullOrEmpty(Sorts))
            {
                PreprocessSorts(queryCriteria);
                queryCriteria.Sorts = DynamicCriteriaHelper.ProcessDapperQuerySort(Sorts, queryCriteria, ConvertSortKey);
            }
            return queryCriteria;
        }

        private SearchFilter ConvertSearchFilter(SearchFilter condition, GetDeviceQueryCriteria queryCriteria)
        {
            var queryKeyNoSpaces = condition.QueryKey.Replace(" ", string.Empty);
            if (string.Equals(queryKeyNoSpaces, "template.Bindings.Any()", System.StringComparison.OrdinalIgnoreCase))
            {
                queryCriteria.TemplateHasBinding = true;
                return null;
            }
            if (_keyMap.ContainsKey(condition.QueryKey.ToLower()))
                condition.QueryKey = _keyMap[condition.QueryKey.ToLower()];
            return condition;
        }

        private void PreprocessSorts(GetDeviceQueryCriteria queryCriteria)
        {
            var stringEqRegex = new Regex("string[.]Equals\\(id(,)\"(.+?)\"\\)\\?0:1");
            var stringEqMatch = stringEqRegex.Match(input: Sorts);
            if (stringEqMatch != null)
            {
                queryCriteria.HighlightedId = stringEqMatch.Groups[2].Value;
                Sorts = stringEqRegex.Replace(input: Sorts, replacement: "d.highlighted_id");
            }
        }

        private string ConvertSortKey(string key, string order, GetDeviceQueryCriteria queryCriteria)
        {
            if (_keyMap.ContainsKey(key.ToLower()))
                return _keyMap[key.ToLower()];
            return key;
        }
    }

    public class GetDeviceQueryCriteria : QueryCriteria
    {
        public bool? TemplateHasBinding { get; set; }
        public string HighlightedId { get; set; }
    }
}
