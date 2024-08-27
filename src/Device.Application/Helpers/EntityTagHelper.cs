using System;
using System.Collections.Generic;
using System.Linq;
using Device.Domain.Entity;

namespace Device.Application.Helper
{
    public static class EntityTagHelper
    {
        public static IList<EntityTagDb> GetEntityTags(
            string entityType,
            IEnumerable<long> tagIds,
            IDictionary<string, object> entityId)
        {
            return tagIds.Select(tagId => new EntityTagDb
            {
                EntityType = entityType,
                TagId = tagId,
                EntityIdInt = (int?)entityId[nameof(EntityTagDb.EntityIdInt)],
                EntityIdString = (string)entityId[nameof(EntityTagDb.EntityIdString)],
                EntityIdGuid = (Guid?)entityId[nameof(EntityTagDb.EntityIdGuid)],
                EntityIdLong = (long?)entityId[nameof(EntityTagDb.EntityIdLong)],
            }).ToList();
        }

        public static IDictionary<string, object> GetEntityId(object entityId)
        {
            return entityId switch
            {
                int intValue =>
                                new Dictionary<string, object>()
                                {
                                    { nameof(EntityTagDb.EntityIdInt), intValue },
                                    { nameof(EntityTagDb.EntityIdString), null },
                                    { nameof(EntityTagDb.EntityIdGuid), null },
                                    { nameof(EntityTagDb.EntityIdLong), null }
                                },
                long longValue =>
                                new Dictionary<string, object>()
                                {
                                    { nameof(EntityTagDb.EntityIdInt), null },
                                    { nameof(EntityTagDb.EntityIdString), null },
                                    { nameof(EntityTagDb.EntityIdGuid), null },
                                    { nameof(EntityTagDb.EntityIdLong), longValue }
                                },
                Guid uuidValue =>
                                new Dictionary<string, object>()
                                {
                                    { nameof(EntityTagDb.EntityIdInt), null },
                                    { nameof(EntityTagDb.EntityIdString), null },
                                    { nameof(EntityTagDb.EntityIdGuid), uuidValue },
                                    { nameof(EntityTagDb.EntityIdLong), null }
                                },
                string strValue =>
                                new Dictionary<string, object>()
                                {
                                    { nameof(EntityTagDb.EntityIdInt), null },
                                    { nameof(EntityTagDb.EntityIdString), strValue },
                                    { nameof(EntityTagDb.EntityIdGuid), null },
                                    { nameof(EntityTagDb.EntityIdLong), null }
                                },
                _ => throw new InvalidCastException(),
            };
        }
    }
}