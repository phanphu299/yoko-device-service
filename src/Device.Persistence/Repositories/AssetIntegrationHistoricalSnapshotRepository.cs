
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.Repository;
using Device.Domain.Entity;
using System.Linq;

namespace Device.Persistence.Repository
{
    public class AssetIntegrationHistoricalSnapshotRepository : IAssetIntegrationHistoricalSnapshotRepository
    {
        private readonly IAssetIntegrationTimeSeriesRepository _assetIntegrationTimeSeries;
        public AssetIntegrationHistoricalSnapshotRepository(IAssetIntegrationTimeSeriesRepository assetIntegrationTimeSeries)
        {
            _assetIntegrationTimeSeries = assetIntegrationTimeSeries;
        }

        public virtual async Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout, string gapfillFunction)
        {
            var data = await _assetIntegrationTimeSeries.QueryDataAsync(timezoneOffset, timeStart, timeEnd, "1 hour", "avg", metrics, timeout, gapfillFunction, 0);
            return data.GroupBy(x => new { x.AssetId, x.AttributeId })
                  .Select(x => x.OrderByDescending(x => x.UnixTimestamp).First());

        }
    }
}
