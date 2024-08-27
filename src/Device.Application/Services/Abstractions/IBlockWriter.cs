using System;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IBlockWriter
    {
        bool CanApply(IBlockOperation operation);
        Task<Guid> WriteValueAsync(IBlockContext context, params BlockDataRequest[] values);
    }
}