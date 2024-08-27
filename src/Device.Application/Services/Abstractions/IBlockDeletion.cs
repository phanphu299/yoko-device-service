using System;
using System.Threading.Tasks;

namespace Device.Application.Service.Abstraction
{
    public interface IBlockDeletion
    {
        bool CanApply(IBlockOperation operation);
        Task<Guid> DeleteValuesAsync(IBlockContext context);
    }
}