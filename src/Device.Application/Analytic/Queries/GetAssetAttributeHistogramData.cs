using System;
using System.Collections.Generic;
using Device.Application.Analytic.Query.Model;
using Device.Application.Historical.Query;
using MediatR;

namespace Device.Application.Analytic.Query
{
    public class GetAssetAttributeHistogramData : IRequest<IEnumerable<AssetHistogramDataDto>>
    {
        public string TimezoneOffset { get; set; } = "+00:00";
        public int TimeoutInSecond { get; set; }
        public IEnumerable<AssetAttributeHistogramData> Assets { get; set; }
        public GetAssetAttributeHistogramData()
        {
            Assets = new List<AssetAttributeHistogramData>();
        }
    }
    public class AssetAttributeHistogramData : AssetAttributeSeries
    {
        public double BinSize { get; set; }
        public string TimezoneOffset { get; set; }

        public new long Start { get; set; }
        public new long End { get; set; }

        public AssetAttributeHistogramData() : base()
        {
            AttributeIds = new List<Guid>();
            Aggregate = "avg";
        }
    }
}