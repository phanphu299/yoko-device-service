using System;
using System.Collections.Generic;
using Device.Application.AlarmRule.Model;
using MediatR;

namespace Device.Application.AlarmRule.Query
{
    public class GetAlarmRuleAssetAttributeDependency : IRequest<IEnumerable<AlarmRuleAssetAttributeDto>>
    {
        public Guid[] AttributeIds { get; }
        public GetAlarmRuleAssetAttributeDependency(Guid[] attributeIds)
        {
            AttributeIds = attributeIds;
        }
    }
}