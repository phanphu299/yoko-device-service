using System;

namespace Device.Domain.Entity
{
    public class AttributeDetail
    {
        public Guid? AttributeId { get; set; }
        public string AttributeName { get; set; }
        public int? UomId { get; set; }
        public string UomName { get; set; }
        public string UomAbbreviation { get; set; }
    }
}