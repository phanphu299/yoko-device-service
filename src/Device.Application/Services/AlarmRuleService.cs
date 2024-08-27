using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.AlarmRule.Model;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;

namespace Device.Application.Service
{
    public class AlarmRuleService : IAlarmRuleService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;
        private readonly ILoggerAdapter<AlarmRuleService> _logger;

        public AlarmRuleService(IHttpClientFactory httpClientFactory, ITenantContext tenantContext, ILoggerAdapter<AlarmRuleService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<IEnumerable<AlarmRuleAssetAttributeDto>> GetAlarmRuleDependencyAsync(Guid[] attributeIds)
        {
            var alarmClient = _httpClientFactory.CreateClient(HttpClientNames.ALARM_SERVICE, _tenantContext);
            var filterCriterial = new[]
            {
                new
                {
                    queryKey = "metricId",
                    operation = "in",
                    queryType = "text",
                    queryValue = string.Join(",", attributeIds)
                },
                new
                {
                    queryKey = "ruleTargetAssetAttribute.AssetAttributeId",
                    operation = "in",
                    queryType = "text",
                    queryValue = string.Join(",", attributeIds)
                }
            };
            var searchContent = new
            {
                Filter = new { Or = filterCriterial }.ToJson(),
                PageSize = int.MaxValue,
                Fields = "id,name".Split(",")
            }.ToJson();
            var response = await alarmClient.PostAsync("alm/rules/search", new StringContent(searchContent, System.Text.Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                var body = content.Deserialize<BaseSearchResponse<AlarmRuleAssetAttributeDto>>();
                return body.Data;
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Calling to alarm-service failed.\r\n Request: {searchContent}.\r\nResponse: {content}");
            }
            return Array.Empty<AlarmRuleAssetAttributeDto>();
        }
    }
}
