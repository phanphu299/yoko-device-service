using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.AlarmRule.Model;
using Device.Application.AlarmRule.Query;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Analytic.Query.Handler
{
    public class GetAlarmRuleAssetAttributeDependencyHandler : IRequestHandler<GetAlarmRuleAssetAttributeDependency, IEnumerable<AlarmRuleAssetAttributeDto>>
    {
        private readonly IAlarmRuleService _alarmRuleService;
        public GetAlarmRuleAssetAttributeDependencyHandler(IAlarmRuleService alarmRuleService)
        {
            _alarmRuleService = alarmRuleService;
        }
        public Task<IEnumerable<AlarmRuleAssetAttributeDto>> Handle(GetAlarmRuleAssetAttributeDependency request, CancellationToken cancellationToken)
        {
            return _alarmRuleService.GetAlarmRuleDependencyAsync(request.AttributeIds);
        }
    }
}