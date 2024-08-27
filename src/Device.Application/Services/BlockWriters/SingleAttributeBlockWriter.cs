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
    public class SingleAttributeBlockWriter : BaseBlockWriter
    {
        public SingleAttributeBlockWriter(IBlockWriter next,
                                    IServiceProvider serviceProvider) : base(next, serviceProvider)
        {
        }

        protected override BlockOperator CurrentBlockOperator => BlockOperator.WriteSingleAttributeValue;

        protected override async Task<Guid> ExecuteOperationAsync(IBlockContext context, params BlockDataRequest[] values)
        {
            using (var scope = _serviceProvider.CreateNewScope())
            {
                var (assetId, assetAttributeId) = await GetAssetInformationAsync(context);
                var blockExecutionRepository = scope.ServiceProvider.GetService<IBlockExecutionRepository>();
                await blockExecutionRepository.SetAssetAttributeValueAsync(assetId, assetAttributeId, values);
                return assetAttributeId;
            }
        }
    }
}
