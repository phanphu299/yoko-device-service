using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class FunctionBlockSnippet : IEntity<Guid>
    {
        public FunctionBlockSnippet()
        {
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string TemplateCode { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }
}
