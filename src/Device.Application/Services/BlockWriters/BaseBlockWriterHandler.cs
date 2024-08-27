using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using Newtonsoft.Json.Linq;

namespace Device.Application.Service
{
    public abstract class BaseBlockWriterHandler<T> : IFunctionBlockWriterHandler where T : FunctionBlockOutputBinding
    {
        private IFunctionBlockWriterHandler _next;
        protected IServiceProvider _serviceProvider;
        public BaseBlockWriterHandler(IFunctionBlockWriterHandler next, IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }
        public bool CanApply(string type)
        {
            return string.Equals(type, OutputType, StringComparison.InvariantCultureIgnoreCase);
        }

        public Task<Guid> HandleWriteValueAsync(string type, IDictionary<string, object> payload, IBlockContext context)
        {
            if (CanApply(type))
            {
                return ExecuteWriteAsync(ParseBinding(payload), context);
            }
            else if (_next != null)
            {
                return _next.HandleWriteValueAsync(type, payload, context);
            }
            else
            {
                throw new SystemNotSupportedException(detailCode: MessageConstants.DATA_TYPE_NOT_SUPPORTED);
            }
        }
        protected abstract string OutputType { get; }
        protected virtual T ParseBinding(IDictionary<string, object> content)
        {
            return JObject.FromObject(content).ToObject<T>();
        }
        protected abstract Task<Guid> ExecuteWriteAsync(T outputBinding, IBlockContext context);
    }
}