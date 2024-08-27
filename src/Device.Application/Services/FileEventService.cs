using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.UserContext.Abstraction;
using Device.Application.Events;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public class FileEventService : IFileEventService
    {
        private readonly IDomainEventDispatcher _dispatcher;
        private readonly ITenantContext _tenantContext;
        private readonly IUserContext _userContext;
        public FileEventService(IDomainEventDispatcher serviceProvider, ITenantContext tenantContext, IUserContext userContext)
        {
            _dispatcher = serviceProvider;
            _tenantContext = tenantContext;
            _userContext = userContext;
        }

        public Task SendExportEventAsync(Guid activityId, string objectType, IEnumerable<string> data)
        {
            var exportEvent = new FileExportEvent(activityId, objectType, data, _tenantContext, _userContext.Upn, _userContext.DateTimeFormat, _userContext.Timezone?.Offset);
            return _dispatcher.SendAsync(exportEvent);
        }

        public Task SendImportEventAsync(string objectType, IEnumerable<string> data, Guid? correlationId = null)
        {
            var importEvent = new FileImportEvent(objectType, data, _tenantContext, _userContext.Upn, _userContext.DateTimeFormat, _userContext.Timezone?.Offset);

            if (correlationId != null)
            {
                importEvent.CorrelationId = correlationId.Value;
            }
            else
            {
                importEvent.CorrelationId = Guid.NewGuid();
            }

            return _dispatcher.SendAsync(importEvent);
        }
    }
}
