using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Device.Persistence.Repository
{
    public class UomPersistenceRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<Uom, int>, IUomRepository, IReadUomRepository
    {
        private readonly DeviceDbContext _context;
        public UomPersistenceRepository(DeviceDbContext context) : base(context)
        {
            _context = context;
        }
        public override IQueryable<Uom> AsQueryable()
        {
            return _context.Uoms.Include(x => x.RefUom)
                                .Include(x => x.EntityTags)
                                .Where(x => !x.EntityTags.Any() || x.EntityTags.Any(a => a.EntityType == EntityTypeConstants.UOM));
        }

        public override IQueryable<Uom> AsFetchable()
        {
            return _context.Uoms.AsNoTracking().Select(x => new Uom { Id = x.Id, Name = x.Name, Abbreviation = x.Abbreviation, CreatedBy = x.CreatedBy, ResourcePath = x.ResourcePath });
        }

        public virtual async Task<bool> RemoveListEntityWithRelationAsync(ICollection<Uom> uoms)
        {
            foreach (Uom item in uoms)
            {
                var element = await FindAsync(item.Id);
                if (element == null)
                    throw new EntityNotFoundException();
                var isRefToOther = await _context.Uoms.Where(x => x.RefId == item.Id).AnyAsync();
                if (isRefToOther)
                {
                    throw new EntityInvalidException(detailCode: MessageConstants.UOM_USING);
                }
                // hard delete the uom
                // az: https://dev.azure.com/thanhtrungbui/yokogawa-ppm/_workitems/edit/15109
                // element.Deleted = true;
                Context.Remove(element);
            }
            //commit delete
            await Context.SaveChangesAsync();
            return true;
        }

        public override async Task<Uom> AddAsync(Uom uom)
        {
            await Context.AddAsync(uom);
            await Context.SaveChangesAsync();
            return uom;
        }

        public async Task<Uom> UpdateAsync(Uom uom)
        {
            var targetUoms = await AsQueryable().FirstOrDefaultAsync(x => x.Id == uom.Id);

            Update(uom, targetUoms);
            Context.Update(targetUoms);
            await Context.SaveChangesAsync();
            return targetUoms;
        }
        protected override void Update(Uom requestObject, Uom targetObject)
        {
            targetObject.Name = requestObject.Name;
            targetObject.LookupCode = requestObject.LookupCode;
            targetObject.RefFactor = requestObject.RefFactor;
            targetObject.RefOffset = requestObject.RefOffset;
            targetObject.CanonicalOffset = requestObject.CanonicalOffset;
            targetObject.CanonicalFactor = requestObject.CanonicalFactor;
            targetObject.Abbreviation = requestObject.Abbreviation;
            targetObject.Description = requestObject.Description;
            targetObject.RefId = requestObject.RefId;
            targetObject.UpdatedUtc = System.DateTime.UtcNow;
            targetObject.System = false;
        }

        public async Task RetrieveAsync(IEnumerable<Uom> uoms)
        {
            _context.Database.SetCommandTimeout(RetrieveConstants.TIME_OUT);
            var systemUomIds = _context.Uoms.AsQueryable().AsNoTracking().AsEnumerable()
                                            .Where(x => uoms.Any(u => u.Id == x.Id)).Select(x => x.Id).ToList();
            var systemUoms = uoms.Where(x => systemUomIds.Contains(x.Id));
            var userUoms = uoms.Where(x => !systemUomIds.Contains(x.Id));

            _context.Uoms.UpdateRange(systemUoms);

            // Disable all constraints (because uoms has a fk reference to itself)
            var disableSql = $"ALTER TABLE uoms DISABLE TRIGGER ALL;";
            await _context.Database.ExecuteSqlRawAsync(disableSql);
            await _context.Uoms.AddRangeAsync(userUoms);
            // Need save change before enable constraint
            await _context.SaveChangesAsync();

            // Re-enable all constraints
            var enableSql = $"ALTER TABLE uoms ENABLE TRIGGER ALL;";
            await _context.Database.ExecuteSqlRawAsync(enableSql);
            // Query to update sequence of column id identity
            var maxId = uoms.Any() ? uoms.Max(x => x.Id) : 0;
            var updateSeq = $"select setval('uoms_id_seq', greatest(coalesce((select max(id)+1 from uoms), 1), {maxId + 1}), false);";
            await _context.Database.ExecuteSqlRawAsync(updateSeq);
        }
    }
}
