using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Service.Dapper.Model;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public abstract class BlockInformation
    {
        protected Task<(Guid, Guid)> GetAssetInformationAsync(IBlockContext context)
        {
            var singleAttribute = context.Attribute as ISingleBlockAttributeContext;
            return Task.FromResult((context.AssetId, singleAttribute.AttributeId));
        }

        protected Task<(Guid, Guid, QueryCriteria)> GetAssetTableQueryInformationAsync(IBlockContext context)
        {
            var assetTable = context.Table as AssetTableContext;
            return Task.FromResult((context.AssetId, assetTable.TableId, assetTable.QueryCriteria));
        }

        protected Task<(Guid, Guid, IEnumerable<IDictionary<string, object>>)> GetAssetTableUpsertInformationAsync(IBlockContext context)
        {
            var assetTable = context.Table as AssetTableContext;
            return Task.FromResult((context.AssetId, assetTable.TableId, assetTable.Data));
        }

        protected Task<(Guid, Guid, string, AggregationCriteria)> GetAssetTableAggregationInformationAsync(IBlockContext context)
        {
            var assetTable = context.Table as AssetTableContext;
            return Task.FromResult((context.AssetId, assetTable.TableId, assetTable.ColumnName, assetTable.AggregationCriteria));
        }

        protected Task<(Guid, Guid, IEnumerable<object>)> GetAssetTableDeleteInformationAsync(IBlockContext context)
        {
            var assetTable = context.Table as AssetTableContext;
            return Task.FromResult((context.AssetId, assetTable.TableId, assetTable.Ids));
        }
    }
}