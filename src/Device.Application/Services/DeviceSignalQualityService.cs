using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.Application.SignalQuality.Command.Model;
using Device.ApplicationExtension.Extension;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Device.Application.Service
{
    public class DeviceSignalQualityService : IDeviceSignalQualityService
    {
        private readonly ICache _cache;
        private readonly ITenantContext _tenantContext;
        private readonly IDeviceSignalQualityRepository _signalQualityRepository;

        public DeviceSignalQualityService(ICache cache, ITenantContext tenantContext, IDeviceSignalQualityRepository signalQualityRepository)
        {
            _cache = cache;
            _tenantContext = tenantContext;
            _signalQualityRepository = signalQualityRepository;
        }

        public async Task<SignalQualityDto[]> GetAllSignalQualityAsync()
        {
            var key = CacheKey.DEVICE_SIGNAL_QUALITY_CODES.GetCacheKey(_tenantContext.ProjectId);
            var signalQualities = await _cache.GetAsync<SignalQualityDto[]>(key);
            if (signalQualities == null)
            {
                var deviceSignalQualities = await _signalQualityRepository.GetAllSignalQualityAsync();
                if (deviceSignalQualities.Any())
                {
                    signalQualities = deviceSignalQualities.Select(x => new SignalQualityDto(x)).ToArray();
                    await _cache.StoreAsync(key, signalQualities);
                }
            }

            return signalQualities;
        }
    }
}