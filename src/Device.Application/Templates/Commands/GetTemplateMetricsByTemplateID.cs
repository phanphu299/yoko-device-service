using System;
using System.Collections.Generic;
using Device.Application.TemplateDetail.Command.Model;
using MediatR;

namespace Device.Application.Template.Command
{
    public class GetTemplateMetricsByTemplateId : IRequest<IEnumerable<GetTemplateDetailsDto>>
    {
        public Guid Id { get; set; }
        public bool IsIncludeDisabledMetric { get; set; }
        public GetTemplateMetricsByTemplateId(Guid id, bool isIncludeDisabledMetric)
        {
            Id = id;
            IsIncludeDisabledMetric = isIncludeDisabledMetric;
        }
    }
}
