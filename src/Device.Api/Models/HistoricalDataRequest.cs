using System.Collections.Generic;

namespace Device.Api.Model
{
    public class HistoricalDataRequest
    {
        public long Start { get; set; }
        public long End { get; set; }
        public int Interval { get; set; }
        public string Aggregate { get; set; }
        public IEnumerable<string> Metrics { get; set; }

        public HistoricalDataRequest()
        {
            Metrics = new List<string>();
        }
    }
}
