using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using Device.Application.Constant;
using Device.Application.DbConnections;
using Device.Application.Repositories;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Device.Persistence.Repository
{
    public class FunctionBlockTemplateRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<FunctionBlockTemplate, Guid>, IFunctionBlockTemplateRepository, IReadFunctionBlockTemplateRepository
    {
        private readonly DeviceDbContext _context;
        private readonly string[] _retrieveTables = {
            "function_block_templates",
            "function_block_template_nodes"
        };
        public FunctionBlockTemplateRepository(DeviceDbContext context) : base(context)
        {
            _context = context;
        }

        public override IQueryable<FunctionBlockTemplate> AsQueryable()
        {
            return _context.FunctionBlockTemplates.Include(x => x.Nodes);
        }

        public override IQueryable<FunctionBlockTemplate> AsFetchable()
        {
            return _context.FunctionBlockTemplates.Include(x => x.Nodes).ThenInclude(x => x.FunctionBlock).ThenInclude(x => x.Bindings)
                                                    .AsNoTracking()
                                                    .Select(x => new FunctionBlockTemplate
                                                    {
                                                        Id = x.Id,
                                                        Name = x.Name,
                                                        CreatedUtc = x.CreatedUtc,
                                                        UpdatedUtc = x.UpdatedUtc,
                                                        //DesignContent = x.DesignContent,
                                                        TriggerContent = x.TriggerContent,
                                                        TriggerType = x.TriggerType,
                                                        Nodes = x.Nodes,
                                                        Version = x.Version
                                                    });
        }

        protected override void Update(FunctionBlockTemplate requestObject, FunctionBlockTemplate targetObject)
        {
            targetObject.Name = requestObject.Name;
            targetObject.UpdatedUtc = DateTime.UtcNow;
            targetObject.DesignContent = requestObject.DesignContent;
            targetObject.Content = requestObject.Content;
            targetObject.Deleted = requestObject.Deleted;
            targetObject.TriggerType = requestObject.TriggerType;
            targetObject.TriggerContent = requestObject.TriggerContent;
            targetObject.Version = requestObject.Version;
        }

        public override Task<FunctionBlockTemplate> FindAsync(Guid id)
        {
            return AsQueryable().AsNoTracking()
             .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<FunctionBlockTemplate> AddEntityAsync(FunctionBlockTemplate entity)
        {
            try
            {
                await _context.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (DbUpdateException ex)
            {
                throw new GenericProcessFailedException(detailCode: MessageConstants.DATABASE_QUERY_FAILED, innerException: ex);
            }
        }

        public async Task<FunctionBlockTemplate> UpdateEntityAsync(FunctionBlockTemplate entity)
        {
            try
            {
                var trackingEntity = await AsQueryable().Where(a => a.Id == entity.Id).FirstOrDefaultAsync();
                if (trackingEntity == null)
                    throw new EntityNotFoundException();

                Update(entity, trackingEntity);
                await UpdateNodesAsync(entity, trackingEntity);
                return entity;
            }
            catch (DbUpdateException ex)
            {
                throw new GenericProcessFailedException(detailCode: MessageConstants.DATABASE_QUERY_FAILED, innerException: ex);
            }
        }

        private async Task UpdateNodesAsync(FunctionBlockTemplate requestObject, FunctionBlockTemplate targetObject)
        {
            var newConnectorNodes = new List<FunctionBlockTemplateNode>();
            var newBlockNodes = new List<FunctionBlockTemplateNode>();
            var usingNodes = new List<FunctionBlockTemplateNode>();
            var blockTemplateNodeIds = targetObject.Nodes.Select(x => x.Id);
            // var mappingNodes = await _context.FunctionBlockNodeMappings.AsQueryable().Where(x => x.BlockTemplateNodeId != null
            //                                                                                   //   && x.AssetMarkupName != null
            //                                                                                   && blockTemplateNodeIds.Contains((Guid)x.BlockTemplateNodeId))
            //                                                                         .ToListAsync();

            foreach (var node in requestObject.Nodes)
            {
                // var trackingNode = targetObject.Nodes.FirstOrDefault(x => x.Id == node.Id); // Cannot use as we rebuild the nodes from design content => id will be changed
                var trackingNode = GetTrackingBlockTemplateNodes(targetObject, node);

                node.BlockTemplateId = requestObject.Id;
                if (trackingNode == null
                    || usingNodes.Any(n => n.Id == trackingNode.Id)
                )
                {
                    if (node.BlockType == BlockTypeConstants.TYPE_BLOCK)
                        newBlockNodes.Add(node);
                    else
                        newConnectorNodes.Add(node);
                }
                else
                {
                    if (node.BlockType != BlockTypeConstants.TYPE_BLOCK) // TYPE_INPUT_CONNECTOR || TYPE_OUTPUT_CONNECTOR
                    {
                        trackingNode.AssetMarkupName = node.AssetMarkupName;
                        trackingNode.TargetName = node.TargetName;
                        trackingNode.Name = node.Name;
                        trackingNode.BlockTemplateId = requestObject.Id;
                        trackingNode.PortId = node.PortId;
                        _context.FunctionBlockTemplateNodes.Update(trackingNode);
                    }
                    // else - We don't need to update data for TYPE_BLOCK now - just mark as using
                    usingNodes.Add(trackingNode);
                }
            }

            // Nghia Removed - We will handle the affected to Block Execution by other functions from Service layer 
            // foreach (var newNode in newConnectorNodes)
            // {
            //     Nghia: As we use First Or Default here, and getting the BlockExecution from it, below code will be false if we have more than 2 Block Execution using the same Template.
            //     var mapping = mappingNodes.Where(x => string.Equals(x.AssetMarkupName, newNode.AssetMarkupName))
            //                               .OrderBy(x => string.Equals(x.TargetName, newNode.TargetName) ? 0 : 1)
            //                               .FirstOrDefault();
            //     if (mapping != null)
            //     {
            //         var newMapping = new FunctionBlockNodeMapping();
            //         newMapping.BlockExecutionId = mapping.BlockExecutionId;
            //         newMapping.AssetId = mapping.AssetId;
            //         newMapping.AssetName = mapping.AssetName;
            //         newMapping.AssetMarkupName = mapping.AssetMarkupName;
            //         newMapping.TargetName = newNode.TargetName;
            //         newMapping.BlockTemplateNodeId = newNode.Id;
            //         newMapping.BlockTemplateNode = newNode;
            //         newMapping.Value = string.Equals(mapping.TargetName, newNode.TargetName)
            //                             ? mapping.Value
            //                             : null;  // Will be mapping later
            //         await _context.FunctionBlockNodeMappings.AddAsync(newMapping);
            //     }
            // }
            // foreach (var newNode in newBlockNodes)
            // {
            //     var newMappings = mappingNodes.Select(m => m.BlockExecutionId).Distinct()
            //                                 .Select(blockExecutionId => new FunctionBlockNodeMapping
            //                                 {
            //                                     BlockExecutionId = blockExecutionId,
            //                                     BlockTemplateNodeId = newNode.BlockTemplateId,
            //                                     BlockTemplateNode = newNode
            //                                 });
            //     await _context.FunctionBlockNodeMappings.AddRangeAsync(newMappings);
            // }

            var removeNodes = targetObject.Nodes.Except(usingNodes);
            // var removeMappings = mappingNodes.Where(m => removeNodes.Any(n => n.Id == m.BlockTemplateNodeId));

            // _context.FunctionBlockNodeMappings.RemoveRange(removeMappings);
            _context.FunctionBlockTemplateNodes.RemoveRange(removeNodes);
            await _context.FunctionBlockTemplateNodes.AddRangeAsync(newConnectorNodes.Union(newBlockNodes));
        }

        private FunctionBlockTemplateNode GetTrackingBlockTemplateNodes(FunctionBlockTemplate targetObject, FunctionBlockTemplateNode node)
        {
            var queryTrackingNode = targetObject.Nodes.Where(x => x.BlockType == node.BlockType);
            switch (node.BlockType)
            {
                case BlockTypeConstants.TYPE_BLOCK:
                    queryTrackingNode = queryTrackingNode.Where(x => x.Id == node.Id);
                    break;
                default:
                    queryTrackingNode = queryTrackingNode
                                            .OrderBy(x => string.Equals(x.Name, node.Name, StringComparison.InvariantCultureIgnoreCase) ? 0 : 1)
                                            .ThenBy(x => x.PortId == node.PortId ? 0 : 1)  // Prioritize based on PortId (2 node has same name like 2 double input with default = 0)
                                            .ThenBy(x => x.FunctionBlockId == node.FunctionBlockId ? 0 : 1) // Then Prioritize based on FunctionBlockId
                                            ;
                    break;
            }
            return queryTrackingNode.FirstOrDefault();
        }

        public async Task<bool> RemoveEntityAsync(FunctionBlockTemplate entity)
        {
            var trackingEntity = await AsQueryable().Where(a => a.Id == entity.Id).FirstOrDefaultAsync();

            if (trackingEntity == null)
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);

            _context.FunctionBlockTemplates.Remove(trackingEntity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task RetrieveAsync(IEnumerable<FunctionBlockTemplate> templates)
        {
            _context.Database.SetCommandTimeout(RetrieveConstants.TIME_OUT);
            StringBuilder builder = new StringBuilder();
            foreach (var table in _retrieveTables)
            {
                builder.Append($"ALTER TABLE {table} DISABLE TRIGGER ALL;");
            }
            var triggerSql = builder.ToString();
            await _context.Database.ExecuteSqlRawAsync(triggerSql);
            await _context.FunctionBlockTemplates.AddRangeAsync(templates);
            await _context.SaveChangesAsync();
            triggerSql = triggerSql.Replace("DISABLE", "ENABLE");
            await _context.Database.ExecuteSqlRawAsync(triggerSql);
        }
    }
}