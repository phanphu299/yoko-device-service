namespace Device.Application.Service
{
    public class AggregationCriteria
    {
        public string AggregationType { get; set; }
        public string FilterName { get; set; }
        public string FilterOperation { get; set; }
        public object FilterValue { get; set; }

        public AggregationCriteria(string aggregationType, string filterName, string filterOperation, object filterValue)
        {
            AggregationType = aggregationType;
            FilterName = filterName;
            FilterOperation = filterOperation;
            FilterValue = filterValue;
        }
    }
}