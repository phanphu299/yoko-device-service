using System;

namespace Device.Application.Model
{
    public class DeleteAssetAttribute
    {
        public bool ForceDelete { get; set; }
        public Guid[] Ids { get; set; }
    }
}
