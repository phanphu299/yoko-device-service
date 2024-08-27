using Device.Application.Repository;
using Device.Persistence.DbContext;
using AHI.Infrastructure.Repository;
namespace Device.Persistence.Repository
{
    public class BlockFunctionUnitOfWork : BaseUnitOfWork, IBlockFunctionUnitOfWork
    {
        public IFunctionBlockExecutionRepository FunctionBlockExecutions { get; }
        public IReadFunctionBlockExecutionRepository ReadFunctionBlockExecutions { get; }
        public IFunctionBlockRepository FunctionBlocks { get; }
        public IReadFunctionBlockRepository ReadFunctionBlocks { get; }
        public IFunctionBlockTemplateRepository FunctionBlockTemplates { get; }
        public IBlockCategoryRepository BlockCategories { get; }
        public IBlockSnippetRepository BlockSnippets { get; }

        public BlockFunctionUnitOfWork(IBlockCategoryRepository blockCategoryRepository, IFunctionBlockTemplateRepository functionBlockTemplateRepository, IFunctionBlockExecutionRepository blockFunctionRepository, DeviceDbContext dbContext, IFunctionBlockRepository functionBlockRepository, IBlockSnippetRepository blockSnippets, IReadFunctionBlockExecutionRepository readFunctionBlockExecutions, IReadFunctionBlockRepository readFunctionBlocks)
            : base(dbContext)
        {
            BlockCategories = blockCategoryRepository;
            FunctionBlockTemplates = functionBlockTemplateRepository;
            FunctionBlockExecutions = blockFunctionRepository;
            FunctionBlocks = functionBlockRepository;
            BlockSnippets = blockSnippets;
            ReadFunctionBlockExecutions = readFunctionBlockExecutions;
            ReadFunctionBlocks = readFunctionBlocks;
        }
    }
}