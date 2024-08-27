using AHI.Infrastructure.Repository.Generic;

namespace Device.Application.Repository
{
    public interface IBlockFunctionUnitOfWork : IUnitOfWork
    {
        IFunctionBlockExecutionRepository FunctionBlockExecutions { get; }
        IReadFunctionBlockExecutionRepository ReadFunctionBlockExecutions { get; }
        IFunctionBlockTemplateRepository FunctionBlockTemplates { get; }
        IFunctionBlockRepository FunctionBlocks { get; }
        IReadFunctionBlockRepository ReadFunctionBlocks { get; }
        IBlockCategoryRepository BlockCategories { get; }
        IBlockSnippetRepository BlockSnippets { get; }
    }
}