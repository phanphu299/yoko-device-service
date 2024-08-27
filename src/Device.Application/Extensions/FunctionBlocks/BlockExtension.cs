using System;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public static class BlockExtension
    {
        public static IBlockContext Asset(this IBlockContext context, Guid assetId)
        {
            context.SetAssetId(assetId);
            return context;
        }
        // public static IChildBlockContext Child(this IBlockContext context, string childAssetName)
        // {
        //     var childBlockContext = new ChildBlockContext(context);
        //     childBlockContext.SetBlockEngine(context.BlockEngine);
        //     childBlockContext.SetAssetName(childAssetName);
        //     return childBlockContext;
        // }
        // public static IBlockHttpContext WithHttp(this IBlockContext context, string httpEndpoint)
        // {
        //     var httpContext = new BlockHttpContext(context);
        //     httpContext.SetBlockEngine(context.BlockEngine);
        //     httpContext.SetEndpoint(httpEndpoint);
        //     context.SetHttpContext(httpContext);
        //     return httpContext;
        // }
        public static IBlockContext Attribute(this IBlockContext context, Guid attributeId)
        {
            var blockAttribute = new SingleBlockAttributeContext();
            blockAttribute.SetAttributeId(attributeId);
            context.SetAttribute(blockAttribute);
            return context;
        }
        // public static IBlockContext Attributes(this IBlockContext context, params string[] attributeNames)
        // {
        //     var blockAttribute = new MultipleBlockAttributeContext();
        //     blockAttribute.SetAttributeNames(attributeNames);
        //     context.SetAttribute(blockAttribute);
        //     return context;
        // }

        public static IBlockContext Table(this IBlockContext context, Guid tableId)
        {
            IAssetTableContext tableContext = null;
            if (context.Table == null)
                tableContext = new AssetTableContext();
            else
                tableContext = context.Table as AssetTableContext;

            tableContext.SetTableId(tableId);
            context.SetTable(tableContext);

            return context;
        }

        public static IBlockContext Column(this IBlockContext context, string columnName)
        {
            IAssetTableContext tableContext = null;
            if (context.Table == null)
                tableContext = new AssetTableContext();
            else
                tableContext = context.Table as AssetTableContext;

            tableContext.SetTableColumnName(columnName);
            context.SetTable(tableContext);

            return context;
        }
    }
}