using System;

namespace Device.Application.Block.Command
{
    public class CleanFunctionBlockCache
    {
        public Guid[] Ids { get; set; }
        public CleanFunctionBlockCache(Guid[] ids)
        {
            Ids = ids;
        }
        public CleanFunctionBlockCache(Guid id)
        {
            Ids = new[] { id };
        }
    }
}

