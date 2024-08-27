using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
namespace Device.Persistence.Repository
{
    public class FunctionBlockExecutionRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<Domain.Entity.FunctionBlockExecution, Guid>, IFunctionBlockExecutionRepository, IReadFunctionBlockExecutionRepository
    {
        private readonly DeviceDbContext _context;
        private readonly string[] _retrieveTables = {
            "function_block_execution_node_mappings"
        };
        public FunctionBlockExecutionRepository(DeviceDbContext context) : base(context)
        {
            _context = context;
        }
        public override IQueryable<FunctionBlockExecution> AsFetchable()
        {
            return _context.FunctionBlockExecutions.AsNoTracking().Select(x => new FunctionBlockExecution { Id = x.Id, Name = x.Name, Version = x.Version });
        }

        protected override void Update(FunctionBlockExecution requestObject, FunctionBlockExecution targetObject)
        {
            targetObject.Name = requestObject.Name;
            targetObject.DiagramContent = requestObject.DiagramContent;
            targetObject.TriggerType = requestObject.TriggerType;
            targetObject.TriggerContent = requestObject.TriggerContent;
            targetObject.FunctionBlockId = requestObject.FunctionBlockId;
            targetObject.TemplateId = requestObject.TemplateId;
            targetObject.UpdatedUtc = DateTime.UtcNow;
            targetObject.RunImmediately = requestObject.RunImmediately;
            targetObject.TriggerAssetId = requestObject.TriggerAssetId;
            targetObject.TriggerAssetMarkup = requestObject.TriggerAssetMarkup;
            targetObject.TriggerAttributeId = requestObject.TriggerAttributeId;
            targetObject.Status = requestObject.Status;
            targetObject.Version = requestObject.Version;
            if (!string.IsNullOrEmpty(requestObject.ExecutionContent))
            {
                targetObject.ExecutionContent = requestObject.ExecutionContent;
            }
            if (requestObject.JobId != null)
            {
                targetObject.JobId = requestObject.JobId;
            }
        }
        public override IQueryable<FunctionBlockExecution> AsQueryable()
        {
            return base.AsQueryable().Include(x => x.Template);
        }

        public async Task UpsertBlockNodeMappingAsync(IEnumerable<FunctionBlockNodeMapping> mappings, Guid executionId)
        {
            var requestMarkupNames = new List<string>();
            var oldMappings = await _context.FunctionBlockNodeMappings.Where(x => x.BlockExecutionId == executionId).ToListAsync();

            _context.FunctionBlockNodeMappings.RemoveRange(oldMappings);
            await _context.FunctionBlockNodeMappings.AddRangeAsync(mappings);
        }

        public async Task RemoveMappingAsync(IEnumerable<FunctionBlockNodeMapping> mappings)
        {
            foreach (var mapping in mappings)
            {
                var existsMapping = await _context.FunctionBlockNodeMappings.AsQueryable().AnyAsync(x => x.Id == mapping.Id);
                if (existsMapping)
                {
                    _context.FunctionBlockNodeMappings.Remove(mapping);
                }
            }
        }

        public async Task RetrieveAsync(IEnumerable<FunctionBlockExecution> functionBlockExecutions)
        {
            _context.Database.SetCommandTimeout(RetrieveConstants.TIME_OUT);
            StringBuilder builder = new StringBuilder();
            foreach (var table in _retrieveTables)
            {
                builder.Append($"ALTER TABLE {table} DISABLE TRIGGER ALL;");
            }
            var triggerSql = builder.ToString();
            await _context.Database.ExecuteSqlRawAsync(triggerSql);
            
            await _context.FunctionBlockExecutions.AddRangeAsync(functionBlockExecutions);
            await _context.SaveChangesAsync();
            triggerSql = triggerSql.Replace("DISABLE", "ENABLE");
            await _context.Database.ExecuteSqlRawAsync(triggerSql);
        }
    }
}