using System;
using System.Collections.Generic;
using Device.Application.Historical.Query;

namespace Device.Job.Model
{
    public class GetFullAssetAttributeSeries : GetAssetAttributeSeriesModel
    {
        public IEnumerable<ColumnMapping> ColumnMappings { get; set; }
        public string DateTimeFormat { get; set; }
    }

    public class ColumnMapping
    {
        public string Name { get; set; }
        public Guid? AttributeId { get; set; }
    }
}