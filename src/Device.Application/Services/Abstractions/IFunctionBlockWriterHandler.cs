using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Device.Application.Service.Abstraction
{
    public interface IFunctionBlockWriterHandler
    {
        bool CanApply(string type);
        Task<Guid> HandleWriteValueAsync(string type, IDictionary<string, object> payload, IBlockContext context);
    }
}