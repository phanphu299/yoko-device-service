namespace AHI.Device.Function.Model.ImportModel
{
    public class Metric
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public int DataTypeId { get; set; }
        public string Expression { get; set; }

        public Metric(string name, int dataTypeId, string expression)
        {
            Name = name;
            DataTypeId = dataTypeId;
            Expression = expression;
        }

        // since there exists a defined constructor with parameter,
        // the parameterless constructor need to be explicitly defined for dapper when query
        public Metric()
        {
        }
    }
}
