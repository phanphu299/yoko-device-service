

using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class TemplatePayload : IEntity<int>
    {
        public int Id { get; set; }
        public Guid TemplateId { get; set; }
        public string JsonPayload { get; set; }
        public virtual DeviceTemplate Template { get; set; }
        public virtual ICollection<TemplateDetail> Details { get; set; }
        public TemplatePayload()
        {
            Details = new List<TemplateDetail>();
        }
    }
}
