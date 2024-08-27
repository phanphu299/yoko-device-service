using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Internal;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Device.Persistence.Repository
{
    public class FunctionBlockPersistenceRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<FunctionBlock, Guid>, IFunctionBlockRepository, IReadFunctionBlockRepository
    {
        private readonly DeviceDbContext _context;
        public FunctionBlockPersistenceRepository(DeviceDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<FunctionBlock> AddEntityWithRelationAsync(FunctionBlock block)
        {
            await _context.Set<FunctionBlock>().AddAsync(block);
            int idx = 1;
            foreach (var binding in block.Bindings)
            {
                binding.FunctionBlockId = block.Id;
                binding.CreatedUtc = DateTime.UtcNow;
                binding.SequentialNumber = idx++;
                await _context.Set<FunctionBlockBinding>().AddAsync(binding);
            }
            return block;
        }
        public async Task<FunctionBlock> UpdateEntityWithRelationAsync(Guid id, FunctionBlock requestBlock, IEnumerable<Guid> bindingIds)
        {
            var trackingEntity = await AsQueryable().FirstOrDefaultAsync(x => x.Id == id);
            if (trackingEntity == null)
                return requestBlock;
            Update(requestBlock, trackingEntity);
            _context.Set<FunctionBlock>().Update(trackingEntity);
            int idx = 1;
            foreach (var binding in trackingEntity.Bindings)
            {
                var bindingCheck = requestBlock.Bindings.Where(x => x.Id == binding.Id).FirstOrDefault();
                if (bindingCheck == null)
                {
                    // validation: binding is being used
                    if (bindingIds.Contains(binding.Id))
                        throw new EntityInvalidException(detailCode: MessageConstants.BINDING_IS_BEING_USED);
                    binding.Deleted = true;
                    _context.Set<FunctionBlockBinding>().Update(binding);
                }
                else
                {
                    binding.DataType = bindingCheck.DataType;
                    binding.Key = bindingCheck.Key;
                    binding.DefaultValue = bindingCheck.DefaultValue;
                    binding.Description = bindingCheck.Description;
                    //binding.IsInput = bindingCheck.IsInput;
                    binding.BindingType = bindingCheck.BindingType;
                    binding.SequentialNumber = idx++;
                    _context.Set<FunctionBlockBinding>().Update(binding);
                }
            }

            foreach (var binding in requestBlock.Bindings)
            {
                if (binding.Id != Guid.Empty)
                    continue;
                binding.FunctionBlockId = trackingEntity.Id;
                binding.CreatedUtc = DateTime.UtcNow;
                binding.SequentialNumber = idx++;
                await _context.Set<FunctionBlockBinding>().AddAsync(binding);
            }
            return requestBlock;
        }

        public override IQueryable<FunctionBlock> AsQueryable()
        {
            return _context.FunctionBlocks.Include(x => x.Bindings);
        }
        public override IQueryable<FunctionBlock> AsFetchable()
        {
            return _context.FunctionBlocks.AsNoTracking().Include(x => x.Bindings).Select(x => new FunctionBlock { Id = x.Id, Name = x.Name, Type = x.Type, Bindings = x.Bindings, CategoryId = x.CategoryId });
        }
        public override Task<FunctionBlock> FindAsync(Guid id)
        {
            return AsQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }
        protected override void Update(FunctionBlock requestObject, FunctionBlock targetObject)
        {
            targetObject.Name = requestObject.Name;
            targetObject.BlockContent = requestObject.BlockContent;
            // targetObject.IsActive = requestObject.IsActive;
            targetObject.UpdatedUtc = DateTime.UtcNow;
            targetObject.Deleted = requestObject.Deleted;
            targetObject.CategoryId = requestObject.CategoryId;
            targetObject.Version = requestObject.Version;
        }

        public Task RetrieveAsync(IEnumerable<FunctionBlock> functionBlocks)
        {
            _context.Database.SetCommandTimeout(RetrieveConstants.TIME_OUT);
            return _context.FunctionBlocks.AddRangeAsync(functionBlocks);
        }
    }
}
