using System.Collections.Generic;
using Device.Application.Analytic.Query.Model;
using Device.Application.Historical.Query;
using MediatR;

namespace Device.Application.Analytic.Query
{
    public class GetAssetAttributeCorrelationData : IRequest<IEnumerable<AssetCorrelationDataDto>>
    {
        public string TimezoneOffset { get; set; } = "+00:00";
        public int TimeoutInSecond { get; set; }
        public IEnumerable<AssetAttributeSeries> Assets { get; set; }
    }
}