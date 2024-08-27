using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Device.Application.Constant;
using Device.Application.DbConnections;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
namespace Device.Persistence.Repository
{
    public class BlockCategoryRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<FunctionBlockCategory, Guid>, IBlockCategoryRepository, IReadBlockCategoryRepository
    {
        private readonly DeviceDbContext _context;
        private readonly IDbConnectionResolver _dbConnectionResolver;
        public BlockCategoryRepository(DeviceDbContext context, IDbConnectionResolver dbConnectionResolver) : base(context)
        {
            _context = context;
            _dbConnectionResolver = dbConnectionResolver;
        }
        public override async Task<FunctionBlockCategory> FindAsync(Guid id)
        {
            var category = await _context.FunctionBlockCategories
                .Include(x => x.FunctionBlocks) //.ThenInclude(x => x.Bindings)
                .Include(x => x.Children)
                .FirstOrDefaultAsync(e => e.Id == id);
            // Reverted below changes, which will caused the issues #61742
            // if (category != null)
            // {
            //     // only load the id,name,categoryId of function block, do not load the content
            //     var functionBlocks = await _context.FunctionBlocks.Where(x => x.CategoryId == id).Select(x => new FunctionBlock() { Id = x.Id, Name = x.Name, CategoryId = x.CategoryId, CreatedUtc = x.CreatedUtc, UpdatedUtc = x.UpdatedUtc }).ToListAsync();
            //     category.FunctionBlocks = functionBlocks;
            // }
            return category;
        }

        protected override void Update(FunctionBlockCategory requestObject, FunctionBlockCategory targetObject)
        {
            targetObject.Name = requestObject.Name;
            targetObject.UpdatedUtc = DateTime.UtcNow;
            targetObject.ParentId = requestObject.ParentId;
        }
        public async Task<IEnumerable<FunctionBlockCategoryPath>> GetPathsAsync(Guid categoryId)
        {
            using (var connection = GetDbConnection())
            {
                return await connection.QueryAsync<FunctionBlockCategoryPath>(@$"select
                                                                        id,
                                                                        name,
                                                                        parent_category_id as ParentId,
                                                                        parent_name as ParentName,
                                                                        category_level as CategoryLevel,
                                                                        category_path_id as CategoryPathId,
                                                                        category_path_name as CategoryPathName
                                                                    from find_root_block_category('{categoryId}')");
            }
        }

        public async Task<IEnumerable<FunctionBlockCategoryHierarchy>> HierarchySearchAsync(string name)
        {
            using (var connection = GetDbConnection())
            {
                var dictionary = new Dictionary<Guid, FunctionBlockCategoryHierarchy>();
                var sql = @"select
                            entity_id as EntityId,
                            entity_name as EntityName,
                            entity_is_category as EntityIsCategory,
                            entity_type as EntityType,
                            hierarchy_entity_id as Id,
                            hierarchy_entity_name as Name,
                            hierarchy_entity_is_category as IsCategory,
                            hierarchy_entity_type as Type,
                            hierarchy_entity_parent_category_id as ParentCategoryId
                        from find_block_category_hierarchy_by_name(@searchName)";

                var categories = await connection.QueryAsync<FunctionBlockCategoryHierarchy, CategoryHierarchy, FunctionBlockCategoryHierarchy>(sql, map: (category, hierarchy) =>
                {
                    if (!dictionary.TryGetValue(category.EntityId, out var categoryEntry))
                    {
                        categoryEntry = category;
                        dictionary.Add(categoryEntry.EntityId, categoryEntry);
                    }
                    categoryEntry.Hierarchy.Add(hierarchy);
                    return categoryEntry;
                }, param: new { SearchName = name?.Trim().Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[") });

                return categories.Distinct();
            }
        }
        public Task RetrieveAsync(IEnumerable<FunctionBlockCategory> categories)
        {
            _context.Database.SetCommandTimeout(RetrieveConstants.TIME_OUT);
            return _context.FunctionBlockCategories.AddRangeAsync(categories);
        }
        protected IDbConnection GetDbConnection() => _dbConnectionResolver.CreateConnection(true);
    }
}
