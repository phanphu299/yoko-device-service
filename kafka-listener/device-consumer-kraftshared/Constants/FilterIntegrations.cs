
using Device.Consumer.KraftShared.Model.SearchModel;

namespace Device.Consumer.KraftShared.Constant
{
    public static class FilterIntegrations
    {
        public static SearchFilter[] GetFilters(string channel)
        {
            return new SearchFilter[]
            {
                new("name.ToLower()", channel.ToLower()),
                new("type.ToLower()", "_api", operation: "contains")
            };
        }
    }
}