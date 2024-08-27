using System.Collections.Generic;
using System.Linq;
using Device.Application.FileRequest.Command;
using Device.Application.Constant;
using FluentValidation;
using AHI.Infrastructure.Exception;

namespace Device.Application.FileRequest.Validation
{
    public class ImportFileValidation : AbstractValidator<ImportFile>
    {
        private IEnumerable<string> _allowedObject;
        public ImportFileValidation()
        {
            _allowedObject = typeof(FileEntityConstants).GetFields().Select(f => (string)f.GetValue(null));

            RuleFor(x => x.ObjectType).Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                .Must(IsObjectValid).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);

            RuleFor(x => x.FileNames).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                .DependentRules(() =>
                {
                    RuleForEach(x => x.FileNames).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                });
        }
        
        private bool IsObjectValid(string objectType)
        {
            return _allowedObject.Contains(objectType);
        }
    }
}