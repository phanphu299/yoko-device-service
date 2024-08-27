using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IBlockEngine
    {
        //Task<IEnumerable<string>> GetAllChildrenAsync(string assetName);
        Task<BlockQueryResult> RunAsync(IBlockContext context);
        //Task<Guid> WriteValueAsync(IBlockContext context, params BlockDataRequest[] value);
        //Task CanWriteValueAsync(IBlockContext context, params BlockDataRequest[] value);
        // Task<BlockQueryResult> RunAsync(IChildBlockContext context);
        // Task WriteValueAsync(IChildBlockContext context, params BlockDataRequest[] value);
        // Task CanWriteValueAsync(IChildBlockContext context, params BlockDataRequest[] value);
        Task<T> HttpPostAsync<T>(IBlockHttpContext context);
        Task<T> HttpGetAsync<T>(IBlockHttpContext context);
    }
}