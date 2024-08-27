using System;
using System.Collections.Generic;
using Device.Application.Service;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class AggregateTableData : IRequest<IEnumerable<object>>
    {
        public Guid AssetId { get; set; }
        public Guid Id { get; set; }
        public string ColumnName { get; set; }
        public AggregationCriteria AggregationCriteria { get; set; }

        public AggregateTableData(Guid assetId, Guid id, string columnName, AggregationCriteria aggregationCriteria = null)
        {
            AssetId = assetId;
            Id = id;
            ColumnName = columnName;
            AggregationCriteria = aggregationCriteria;
        }
    }
}