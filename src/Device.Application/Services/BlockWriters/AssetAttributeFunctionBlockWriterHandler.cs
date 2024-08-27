using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Device.Application.Asset.Command.Model;

namespace Device.Application.Service
{
    public class AssetAttributeFunctionBlockWriterHandler : BaseBlockWriterHandler<AssetAttributeBinding>
    {

        public AssetAttributeFunctionBlockWriterHandler(IFunctionBlockWriterHandler next, IServiceProvider serviceProvider) : base(next, serviceProvider)
        {
        }

        protected override string OutputType => BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE;

        protected override Task<Guid> ExecuteWriteAsync(AssetAttributeBinding outputBinding, IBlockContext context)
        {
            var blockWriter = _serviceProvider.GetRequiredService<IBlockWriter>();
            context.Asset(outputBinding.AssetId).Attribute(outputBinding.AttributeId.Value);
            var blockOperation = new BlockOperation();
            blockOperation.SetOperator(Enum.BlockOperator.WriteSingleAttributeValue);
            context.SetBlockOperation(blockOperation);
            BlockDataRequest[] requests = default;
            if (context.IsArrayValue)
            {
                var values = context.Value as IEnumerable<BlockArrayValue>;
                requests = values.Select(x => new BlockDataRequest(x.Value, context.DataType, x.DateTime)).ToArray();
            }
            else
            {
                var targetDateTime = context.DateTime;
                if (targetDateTime == null)
                {
                    targetDateTime = outputBinding.SnapshotDateTime;
                }
                if (targetDateTime == null)
                {
                    targetDateTime = DateTime.UtcNow;
                }
                requests = new[] { new BlockDataRequest(context.Value, context.DataType, targetDateTime.Value) };
            }
            return blockWriter.WriteValueAsync(context, requests);
        }
    }
}