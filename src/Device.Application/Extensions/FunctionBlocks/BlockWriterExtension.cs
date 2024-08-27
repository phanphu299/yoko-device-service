using System.Threading.Tasks;
using System.Collections.Generic;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public static class BlockWriterExtension
    {
        /// <summary>
        /// write value to asset table
        /// </summary>
        public static async Task<IBlockContext> WriteAsync(this IBlockContext context, IEnumerable<IDictionary<string, object>> data)
        {
            var blockOperation = new BlockOperation();
            blockOperation.SetOperator(Enum.BlockOperator.WriteAssetTableData);
            context.SetBlockOperation(blockOperation);
            context.SetTableData(data);
            return await Task.FromResult(context);
        }

        /// <summary>
        /// delete data from asset table
        /// </summary>
        public static async Task<IBlockContext> DeleteAsync(this IBlockContext context, IEnumerable<object> ids)
        {
            var blockOperation = new BlockOperation();
            blockOperation.SetOperator(Enum.BlockOperator.DeleteAssetTableData);
            context.SetBlockOperation(blockOperation);
            context.SetTableIds(ids);
            return await Task.FromResult(context);
        }
    }
}