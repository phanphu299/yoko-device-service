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
    public class QueryDurationAttributeValueBlockExecution : BaseBlockExecution
    {
        public QueryDurationAttributeValueBlockExecution(IBlockExecution next,
                                                    IServiceProvider serviceProvider) : base(next, serviceProvider)
        {
        }

        protected override BlockOperator CurrentBlockOperator => BlockOperator.DurationInSingleAttributeValue;

        protected override async Task<BlockQueryResult> ExecuteOperationAsync(IBlockContext context)
        {
            using (var scope = _serviceProvider.CreateNewScope())
            {
                var (assetId, assetAttributeId) = await GetAssetInformationAsync(context);
                var blockExecutionRepository = scope.ServiceProvider.GetService<IBlockExecutionRepository>();
                var result = await blockExecutionRepository.GetDurationAssetAttributeValueAsync(assetId, assetAttributeId, context.BlockOperation.StartTime, context.BlockOperation.EndTime, context.BlockOperation.FilterOperator, context.BlockOperation.FilterValue, context.BlockOperation.FilterUnit);
                return new BlockQueryResult(result);
            }
        }
    }
}