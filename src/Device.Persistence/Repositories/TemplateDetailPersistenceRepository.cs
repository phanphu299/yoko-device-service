
using System;
using System.Linq;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;
using Device.Application.Constant;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Device.Persistence.Repository
{
    public class TemplateDetailPersistenceRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<TemplateDetail, int>, ITemplateDetailRepository
    {
        private readonly DeviceDbContext _dbContext;
        public IQueryable<TemplateDetail> TemplatesDetails => _dbContext.TemplateDetails;

        public TemplateDetailPersistenceRepository(DeviceDbContext context) : base(context)
        {
            _dbContext = context;
        }

        protected override void Update(TemplateDetail requestObject, TemplateDetail targetObject)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TemplateDetail> AsFetchDeviceMetricQueryable(string deviceId, int metricId, string metricKey)
        {
            var devices = _dbContext.Devices;
            var templates = _dbContext.DeviceTemplates;
            var payloads = _dbContext.TemplatePayloads;
            var details = _dbContext.TemplateDetails.Include(x => x.TemplateKeyType);

            return from device in devices
                   join template in templates on device.TemplateId equals template.Id
                   join payload in payloads on template.Id equals payload.TemplateId
                   join detail in details on payload.Id equals detail.TemplatePayloadId
                   where device.Id == deviceId
                       && (detail.Id == metricId || detail.Key == metricKey)
                       && (detail.TemplateKeyType.Name == TemplateKeyTypeConstants.METRIC || detail.TemplateKeyType.Name == TemplateKeyTypeConstants.AGGREGATION)
                       && detail.Enabled
                   select new Domain.Entity.TemplateDetail
                   {
                       Id = detail.Id,
                       Name = detail.Name,
                       Key = detail.Key,
                       Payload = new Domain.Entity.TemplatePayload
                       {
                           Template = new Domain.Entity.DeviceTemplate
                           {
                               Devices = new List<Domain.Entity.Device>
                                {
                                    new Domain.Entity.Device { Id = device.Id, Name = device.Name }
                                }
                           }
                       }
                   };
        }
    }
}
