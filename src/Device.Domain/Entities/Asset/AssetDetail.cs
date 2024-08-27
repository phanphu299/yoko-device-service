using System;
using System.Collections.Generic;

namespace Device.Domain.Entity
{
    public class AssetDetail
    {
        public Guid AssetId { get; set; }
        public string AssetName { get; set; }
        public ICollection<AttributeDetail> Attributes { get; set; }

        public AssetDetail()
        {
            Attributes = new List<AttributeDetail>();
        }
    }
}