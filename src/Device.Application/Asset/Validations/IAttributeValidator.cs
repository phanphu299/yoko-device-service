using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.Asset.Command;
using Device.Application.Enum;
using FluentValidation.Results;

namespace Device.Application.Validation
{
    public interface IAttributeValidator
    {
        Task<IEnumerable<ValidationFailure>> ValidateAsync(Guid assetId, ValidatAttributeRequest command, IEnumerable<ValidatAttributeRequest> attributes, ValidationAction validationAction);

        bool CanApply(string type);
    }
}
