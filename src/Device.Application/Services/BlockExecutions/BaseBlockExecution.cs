using System;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using Device.Application.BlockFunction.Model;
using Device.Application.Constant;
using Device.Application.Enum;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public abstract class BaseBlockExecution : BlockInformation, IBlockExecution
    {
        private IBlockExecution _next;
        protected readonly IServiceProvider _serviceProvider;

        public BaseBlockExecution(IBlockExecution next,
                                    IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }
        public bool CanApply(IBlockOperation operation)
        {
            return operation.Operator == CurrentBlockOperator;
        }

        public Task<BlockQueryResult> ExecuteAsync(IBlockContext context)
        {
            if (CanApply(context.BlockOperation))
            {
                return ExecuteOperationAsync(context);
            }
            else if (_next != null)
            {
                return _next.ExecuteAsync(context);
            }
            else
            {
                throw new SystemNotSupportedException(detailCode: MessageConstants.BLOCK_FUNCTION_OPERATION_NOT_SUPPORTED);
            }
        }

        protected abstract BlockOperator CurrentBlockOperator { get; }

        /// <summary>
        /// Need to seperate scope inside this function cause it will be run in parallel
        /// </summary>
        protected abstract Task<BlockQueryResult> ExecuteOperationAsync(IBlockContext context);
    }
}