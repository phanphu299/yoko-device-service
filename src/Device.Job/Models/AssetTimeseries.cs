using System.Collections.Generic;
using Device.Application.Historical.Query;

namespace Device.Job.Model
{
    public class AssetTimeseries : AssetAttributeSeries
    {
        public IEnumerable<string> ColumnNames { get; set; }
    }
}