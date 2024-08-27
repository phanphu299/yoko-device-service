using System.Linq;
using AHI.Infrastructure.Repository.Generic;

namespace Device.Application.Repository
{
    public interface ITemplateDetailRepository : IRepository<Domain.Entity.TemplateDetail, int>
    {
        IQueryable<Domain.Entity.TemplateDetail> AsFetchDeviceMetricQueryable(string deviceId, int metricId, string metricKey);
    }
}
