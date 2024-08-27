
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class TemplateKeyType : IEntity<int>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Deleted { get; set; }
        public ICollection<TemplateDetail> TemplateDetails { get; set; }
        public TemplateKeyType()
        {
            TemplateDetails = new List<TemplateDetail>();
        }
    }
}
