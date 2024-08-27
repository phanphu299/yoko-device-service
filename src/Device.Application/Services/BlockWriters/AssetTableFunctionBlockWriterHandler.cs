using System;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using Microsoft.Extensions.DependencyInjection;

namespace Device.Application.Service
{
    public class AssetTableFunctionBlockWriterHandler : BaseBlockWriterHandler<AssetTableBinding>
    {

        public AssetTableFunctionBlockWriterHandler(IFunctionBlockWriterHandler next, IServiceProvider serviceProvider) : base(next, serviceProvider)
        {
        }

        protected override string OutputType => BindingDataTypeIdConstants.TYPE_ASSET_TABLE;

        protected override Task<Guid> ExecuteWriteAsync(AssetTableBinding outputBinding, IBlockContext context)
        {
            var blockWriter = _serviceProvider.GetRequiredService<IBlockWriter>();
            var assetTable = context.Table as AssetTableContext;

            BlockDataRequest[] requests = default;

            if (context.BlockOperation.Operator == Enum.BlockOperator.WriteAssetTableData)
                requests = new[] { new BlockDataRequest(assetTable.Data, context.DataType, DateTime.UtcNow) };
            else if (context.BlockOperation.Operator == Enum.BlockOperator.DeleteAssetTableData)
                requests = new[] { new BlockDataRequest(assetTable.Ids, context.DataType, DateTime.UtcNow) };

            return blockWriter.WriteValueAsync(context, requests);
        }
    }
}