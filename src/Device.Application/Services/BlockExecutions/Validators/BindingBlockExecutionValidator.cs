using System.Threading.Tasks;
using System.Collections.Generic;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Newtonsoft.Json.Linq;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using Device.Application.Model;

namespace Device.Application.Service
{
    public class BindingBlockExecutionValidator : BaseBlockExecutionValidator
    {
        public BindingBlockExecutionValidator(IAssetUnitOfWork unitOfWork, IFunctionBlockExecutionResolver blockExecutionResolver, BaseBlockExecutionValidator nextValidator)
            : base(unitOfWork, blockExecutionResolver, nextValidator)
        {
        }

        public override async Task ValidateAsync()
        {
            if (ValidationContext == null)
                return;

            ValidateBinding(ValidationContext.Inputs);
            ValidateBinding(ValidationContext.Outputs);

            if (NextValidator != null)
            {
                NextValidator.SetValidationContext(ValidationContext);
                await NextValidator.ValidateAsync();
            }
        }

        private void ValidateBinding(IEnumerable<BlockExecutionBindingInformation> bindings)
        {
            foreach (var binding in bindings)
            {
                switch (binding.DataType)
                {
                    case BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE:
                        var assetAttribute = JObject.FromObject(binding.Payload).ToObject<AssetAttributeBinding>();
                        if (!assetAttribute.AttributeId.HasValue)
                        {
                            throw EntityValidationExceptionHelper.GenerateException("AttributeId", ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                        }
                        break;
                    case BindingDataTypeIdConstants.TYPE_ASSET_TABLE:
                        var assetTable = JObject.FromObject(binding.Payload).ToObject<AssetTableBinding>();
                        if (!assetTable.TableId.HasValue)
                        {
                            throw EntityValidationExceptionHelper.GenerateException("assetTable.TableId", ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                        }
                        break;
                }
            }
        }
    }
}