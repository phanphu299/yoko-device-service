using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public abstract class BaseBlockTriggerHandler : IBlockTriggerHandler
    {
        private readonly IBlockTriggerHandler _next;

        public BaseBlockTriggerHandler(IBlockTriggerHandler next)
        {
            _next = next;
        }
        public bool CanHandle(string triggerType)
        {
            return triggerType == TriggerType;
        }
        protected abstract string TriggerType { get; }

        public Task RegisterAsync(Domain.Entity.FunctionBlockExecution block)
        {
            if (CanHandle(block.TriggerType))
            {
                return ProcessRegisterAsync(block);
            }
            else if (_next != null)
            {
                return _next.RegisterAsync(block);
            }
            else
            {
                throw new SystemNotSupportedException(detailCode: MessageConstants.BLOCK_FUNCTION_TRIGGER_TYPE_INVALID);
            }
        }


        public Task UnregisterAsync(Domain.Entity.FunctionBlockExecution block)
        {
            if (CanHandle(block.TriggerType))
            {
                return ProcessUnregisterAsync(block);
            }
            else if (_next != null)
            {
                return _next.UnregisterAsync(block);
            }
            else
            {
                throw new SystemNotSupportedException(detailCode: MessageConstants.BLOCK_FUNCTION_TRIGGER_TYPE_INVALID);
            }
        }
        protected abstract Task ProcessRegisterAsync(Domain.Entity.FunctionBlockExecution block);
        protected abstract Task ProcessUnregisterAsync(Domain.Entity.FunctionBlockExecution block);
    }
    public class MessageResponse
    {
        public Guid Id { get; set; }
        public bool IsSucess { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public IEnumerable<DetailError> Fields { get; set; }
    }
    public class DetailError
    {
        public string Name { get; set; }
        public string ErrorCode { get; set; }
    }
}
