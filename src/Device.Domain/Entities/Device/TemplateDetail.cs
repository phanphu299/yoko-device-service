
using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class TemplateDetail : IEntity<int>
    {
        public int Id { get; set; }
        public int TemplatePayloadId { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public int KeyTypeId { get; set; }
        public bool Enabled { get; set; }
        public string Expression { get; set; }
        public string ExpressionCompile { get; set; }
        public string DataType { get; set; }
        public Guid DetailId { get; set; } = Guid.NewGuid();
        public virtual TemplateKeyType TemplateKeyType { get; set; }
        public virtual TemplatePayload Payload { get; set; }
    }
}
