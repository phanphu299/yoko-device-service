using System.Threading.Tasks;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public class CodeBlockExecutionValidator : BaseBlockExecutionValidator
    {
        public CodeBlockExecutionValidator(IAssetUnitOfWork unitOfWork, IFunctionBlockExecutionResolver blockExecutionResolver, BaseBlockExecutionValidator nextValidator) : base(unitOfWork, blockExecutionResolver, nextValidator)
        {
        }

        public override async Task ValidateAsync()
        {
            if (ValidationContext == null)
                return;

            if (BlockFunctionResolver != null && ValidationContext.Variable != null)
            {
                var blockFunctionInstance = BlockFunctionResolver.ResolveInstance(ValidationContext.BlockTemplateContent?.Content);
                blockFunctionInstance.SetVariable(ValidationContext.Variable);
                await blockFunctionInstance.ExecuteAsync();
            }

            if (NextValidator != null)
            {
                NextValidator.SetValidationContext(ValidationContext);
                await NextValidator.ValidateAsync();
            }
        }
    }
}