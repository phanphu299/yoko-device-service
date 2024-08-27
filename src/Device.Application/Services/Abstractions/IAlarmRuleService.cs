using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.AlarmRule.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IAlarmRuleService
    {
        Task<IEnumerable<AlarmRuleAssetAttributeDto>> GetAlarmRuleDependencyAsync(Guid[] attributeIds);
    }
}