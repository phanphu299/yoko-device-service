using AHI.Infrastructure.SharedKernel.Extension;
using Newtonsoft.Json;

namespace Device.Consumer.KraftShared.Model.SearchModel
{
    public class FilteredSearchQuery
    {
        public bool ClientOverride => true;
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = ushort.MaxValue;
        public string Filter { get; set; } = string.Empty;
        public string[] Fields { get; set; }

        public FilteredSearchQuery(params SearchFilter[] filterObjects) : this(LogicalOp.And, filterObjects)
        {
        }

        public FilteredSearchQuery(LogicalOp LogicOp, params SearchFilter[] filterObjects)
        {
            if (filterObjects.Length == 0)
                return;

            var filterObject = BuildMultiFilter(LogicOp, filterObjects);
            Filter = filterObject.ToJson();
        }

        [JsonIgnore]
        public string AsJsonString => this.ToJson();

        private static object BuildMultiFilter(LogicalOp LogicOp, SearchFilter[] filters)
        {
            if (LogicOp == LogicalOp.Or)
                return new { Or = filters };

            return new { And = filters };
        }

        public enum LogicalOp
        {
            And,
            Or
        }
    }

    public class SearchFilter
    {
        public string QueryKey { get; set; }
        public string QueryValue { get; set; }
        public string Operation { get; set; }
        public string QueryType { get; set; }

        public SearchFilter(string queryKey, string queryValue, string operation = "eq", string queryType = "text")
        {
            QueryKey = queryKey;
            QueryValue = queryValue;
            Operation = operation;
            QueryType = queryType;
        }
    }
}