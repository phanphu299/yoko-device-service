using MediatR;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Asset.Command.Model;
using AHI.Infrastructure.Service.Model;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Extension;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Device.Application.Asset.Command
{
    public class GetAssetHierarchy : BaseCriteria, IRequest<BaseSearchResponse<GetAssetHierarchyDto>>
    {
        public string AssetName { get; set; }
        public IEnumerable<long> TagIds { get; private set; }
        private const string QUERY_TAG = "entitytags.tagid";

        public void SetTagIds()
        {
            TagIds = Array.Empty<long>();
            var defaultFilter = new List<FilterModel>();

            if (!string.IsNullOrEmpty(Filter))
            {
                var originFilter = Filter.ToLower().FromJson<JObject>();
                var token = originFilter.Property("and");

                if (token != null)
                {
                    var jArray = (JArray)token.Value;
                    defaultFilter = jArray.ToObject<List<FilterModel>>() ?? new List<FilterModel>();
                }
                else
                {
                    defaultFilter = new List<FilterModel>() { originFilter.ToObject<FilterModel>() };
                }
            }

            if (defaultFilter != null && defaultFilter.Any())
            {
                var andFilter = string.Join(",", defaultFilter.Where(x => x.QueryKey == QUERY_TAG && x.Operation == "and").Select(x => x.QueryValue));
                var orFilter = string.Join(",", defaultFilter.Where(x => x.QueryKey == QUERY_TAG && x.Operation == "or").Select(x => x.QueryValue));
                TagIds = $"{andFilter},{orFilter}".Split(",").Where(x => !string.IsNullOrWhiteSpace(x)).Select(long.Parse);
            }
        }
    }
}
