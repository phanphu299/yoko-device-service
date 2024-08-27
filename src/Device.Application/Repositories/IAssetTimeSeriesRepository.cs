using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface IAssetTimeSeriesRepository
    {
        Task<Domain.Entity.TimeSeries> GetNearestAssetAttributeAsync(DateTime dateTime, Domain.Entity.HistoricalEntity assetAttribute, string padding);
        Task<double> GetLastTimeDiffAssetAttributeAsync(Domain.Entity.HistoricalEntity assetAttribute);
        Task<double> GetLastValueDiffAssetAttributeAsync(Domain.Entity.HistoricalEntity assetAttribute);
        Task<double> GetTimeDiff2PointsAssetAttributeValueAsync(Domain.Entity.HistoricalEntity assetAttribute, DateTime start, DateTime end);
        Task<double> GetValueDiff2PointsAssetAttributeValueAsync(Domain.Entity.HistoricalEntity assetAttribute, DateTime start, DateTime end);
        Task<double> AggregateAssetAttributesValueAsync(Domain.Entity.HistoricalEntity assetAttribute, DateTime start, DateTime end, string aggregate, string filterOperation, object filterValue);
        Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout, string gapfillFunction, int limit, int? quality = null);
        Task<double> GetDurationAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end, string filterOperation, object filterValue);
        Task<int> GetCountAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end, string filterOperation, object filterValue);
        Task<IEnumerable<Histogram>> GetHistogramAsync(DateTime timeStart, DateTime timeEnd, double binSize, IEnumerable<Domain.Entity.HistoricalEntity> metrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction);
        Task<(IEnumerable<TimeSeries> Series, int TotalCount)> PaginationQueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, Domain.Entity.HistoricalEntity historicalEntity, int timeout, string gapfillFunction, int pageIndex, int pageSize, int? quality = null);
        Task<IEnumerable<Statistics>> GetStatisticsAsync(DateTime timeStart, DateTime timeEnd, IEnumerable<Domain.Entity.HistoricalEntity> metrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction);
    }
}
