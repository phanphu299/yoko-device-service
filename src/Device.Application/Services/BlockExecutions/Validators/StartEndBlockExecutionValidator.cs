using System.Threading.Tasks;
using AHI.Infrastructure.Exception.Helper;
using Device.Application.BlockFunction.Trigger.Model;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Newtonsoft.Json;

namespace Device.Application.Service
{
    public class StartEndBlockExecutionValidator : BaseBlockExecutionValidator
    {
        public StartEndBlockExecutionValidator(IAssetUnitOfWork unitOfWork, IFunctionBlockExecutionResolver blockExecutionResolver, BaseBlockExecutionValidator nextValidator) : base(unitOfWork, blockExecutionResolver, nextValidator)
        {
        }

        public override async Task ValidateAsync()
        {
            if (ValidationContext == null)
                return;

            if (ValidationContext.TriggerType == BlockFunctionTriggerConstants.TYPE_SCHEDULER)
            {
                var schedulerRequest = JsonConvert.DeserializeObject<SchedulerTriggerDto>(ValidationContext.TriggerContent);
                if (schedulerRequest.Start > schedulerRequest.End)
                {
                    throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(schedulerRequest.Start));
                }
            }

            if (NextValidator != null)
            {
                NextValidator.SetValidationContext(ValidationContext);
                await NextValidator.ValidateAsync();
            }
        }
    }
}