using System;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;
using Device.Application.Enum;
using Device.Application.Extension;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Microsoft.Extensions.DependencyInjection;

namespace Device.Application.Service
{
    public class QueryTimeDiff2PointsAttributeValueBlockExecution : BaseBlockExecution
    {
        public QueryTimeDiff2PointsAttributeValueBlockExecution(IBlockExecution next,
                                                    IServiceProvider serviceProvider) : base(next, serviceProvider)
        {
        }

        protected override BlockOperator CurrentBlockOperator => BlockOperator.DifferenceTimeBetween2PointSingleAttributeValue;

        protected override async Task<BlockQueryResult> ExecuteOperationAsync(IBlockContext context)
        {
            using (var scope = _serviceProvider.CreateNewScope())
            {
                var (assetId, assetAttributeId) = await GetAssetInformationAsync(context);
                var blockExecutionRepository = scope.ServiceProvider.GetService<IBlockExecutionRepository>();
                var result = await blockExecutionRepository.GetTimeDiff2PointsAssetAttributeValueAsync(assetId, assetAttributeId, context.BlockOperation.StartTime, context.BlockOperation.EndTime, context.BlockOperation.FilterUnit);
                return new BlockQueryResult(result);
            }
        }
    }
}
