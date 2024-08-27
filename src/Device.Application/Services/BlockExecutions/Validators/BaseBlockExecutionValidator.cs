using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.Application.Model;

namespace Device.Application.Service
{
    public abstract class BaseBlockExecutionValidator
    {
        protected IAssetUnitOfWork UnitOfWork;
        protected IFunctionBlockExecutionResolver BlockFunctionResolver;
        protected BlockExecutionValidationContext ValidationContext;

        protected readonly BaseBlockExecutionValidator NextValidator;

        public BaseBlockExecutionValidator(IAssetUnitOfWork unitOfWork, IFunctionBlockExecutionResolver blockFunctionResolver, BaseBlockExecutionValidator nextValidator)
        {
            UnitOfWork = unitOfWork;
            BlockFunctionResolver = blockFunctionResolver;
            NextValidator = nextValidator;
        }

        public void SetValidationContext(BlockExecutionValidationContext validationContext)
        {
            ValidationContext = validationContext;
        }

        public abstract Task ValidateAsync();
    }

    public class BlockExecutionValidationContext
    {
        public string TriggerType;
        public string TriggerContent;
        public IBlockVariable Variable;
        public FunctionBlockTemplateContent BlockTemplateContent;
        public IEnumerable<BlockExecutionInputInformation> Inputs = Array.Empty<BlockExecutionInputInformation>();
        public IEnumerable<BlockExecutionOutputInformation> Outputs = Array.Empty<BlockExecutionOutputInformation>();

        public BlockExecutionValidationContext(string triggerType, string triggerContent)
        {
            TriggerType = triggerType;
            TriggerContent = triggerContent;
        }

        public void SetBlockTemplateContent(FunctionBlockTemplateContent blockTemplateContent)
        {
            BlockTemplateContent = blockTemplateContent;
        }

        public void SetInputs(IEnumerable<BlockExecutionInputInformation> inputs)
        {
            Inputs = inputs;
        }

        public void SetOutputs(IEnumerable<BlockExecutionOutputInformation> outputs)
        {
            Outputs = outputs;
        }

        public void SetVariable(IBlockVariable variable)
        {
            Variable = variable;
        }
    }
}