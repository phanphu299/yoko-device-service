using System.Collections.Generic;
using Device.Domain.Entity;

namespace Device.Persistence.Extension
{
    public static class EntityTagComparer
    {
        public static readonly IEqualityComparer<EntityTagDb> TagIdComparer = new TagIdEntityComparer<EntityTagDb>();
    }

    public class TagIdEntityComparer<T> : IEqualityComparer<T> where T : EntityTagDb
    {
        public bool Equals(T x, T y)
        {
            return
                x.TagId == y.TagId &&
                x.EntityIdGuid == y.EntityIdGuid &&
                x.EntityIdInt == y.EntityIdInt &&
                x.EntityIdString == y.EntityIdString &&
                x.EntityIdLong == y.EntityIdLong;
        }

        public int GetHashCode(T obj)
        {
            return obj.TagId.GetHashCode();
        }
    }
}