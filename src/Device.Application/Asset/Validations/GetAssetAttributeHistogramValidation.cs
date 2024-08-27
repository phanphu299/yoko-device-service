using FluentValidation;
using System.Linq;
using AHI.Infrastructure.Exception;
using Device.Application.Constant;
using Device.Application.Analytic.Query;

namespace Device.Application.Asset.Validation
{
    public class GetAssetAttributeHistogramValidation : AbstractValidator<GetAssetAttributeHistogramData>
    {
        public GetAssetAttributeHistogramValidation()
        {
            RuleForEach(x => x.Assets).SetValidator(
                new InlineValidator<AssetAttributeHistogramData> {
                    agValidator => agValidator.RuleFor(x => x.GapfillFunction)
                                              .NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                                              .Must(x => PostgresFunction.ALLOW_GAPFILL_FUNCTION.Any(a => a == x)).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID)
                }
            );
            RuleForEach(x => x.Assets).SetValidator(
               new InlineValidator<AssetAttributeHistogramData> {
                    agValidator => agValidator.RuleFor(x => x.TimeGrain)
                                              .Must(x=> PostgresFunction.IsValidGranularity(x)).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID)
               }
           );

            RuleForEach(x => x.Assets).SetValidator(
                new InlineValidator<AssetAttributeHistogramData> {
                    agValidator => agValidator.RuleFor(x => x.Aggregate)
                                              .Must(x => PostgresFunction.ALLOW_AGGREGATE_FUNCTION.Any(a => a == x.ToLowerInvariant())).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID)
                                              .When(x=> !string.IsNullOrEmpty(x.TimeGrain))
                }
            );

            RuleForEach(x => x.Assets).SetValidator(
               new InlineValidator<AssetAttributeHistogramData> {
                    agValidator => agValidator.RuleFor(x => x.BinSize)
                                               .Must(x=> x > 0).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID)
               }
           );
        }
    }
}
