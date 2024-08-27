using System;
using System.Linq;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using Device.Application.BlockFunction.Model;
using Device.Application.Constant;
using Device.Application.Enum;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public abstract class BaseBlockWriter : BlockInformation, IBlockWriter
    {
        private IBlockWriter _next;
        protected readonly IServiceProvider _serviceProvider;

        public BaseBlockWriter(IBlockWriter next,
                                IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }
        public bool CanApply(IBlockOperation operation)
        {
            return operation.Operator == CurrentBlockOperator;
        }

        public Task<Guid> WriteValueAsync(IBlockContext context, params BlockDataRequest[] values)
        {
            if (values == null || !values.Any())
            {
                throw new NullReferenceException($"Can not write null value");
            }
            if (CanApply(context.BlockOperation))
            {
                return ExecuteOperationAsync(context, values);
            }
            else if (_next != null)
            {
                return _next.WriteValueAsync(context, values);
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
        protected abstract Task<Guid> ExecuteOperationAsync(IBlockContext context, params BlockDataRequest[] values);
    }
}