using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface IAssetHistoricalSnapshotRepository
    {
        Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, IEnumerable<HistoricalEntity> metrics, int timeout, string gapfillFunction);
    }
}