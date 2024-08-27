using FluentValidation;
using Device.Application.Historical.Query;
using AHI.Infrastructure.Exception;
using Device.Application.Constant;
using System.Linq;

namespace Device.Application.Asset.Validation
{
    public class PaginationGetAssetAttributesSeriesValidation : AbstractValidator<PaginationGetAssetAttributeSeries>
    {
        public PaginationGetAssetAttributesSeriesValidation()
        {
            RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(0).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.PageSize).GreaterThanOrEqualTo(0).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);

            RuleForEach(x => x.Assets).SetValidator(
                new InlineValidator<AssetAttributeSeries> {
                    agValidator => agValidator.RuleFor(x => x.GapfillFunction)
                                              .NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                                              .Must(x => PostgresFunction.ALLOW_GAPFILL_FUNCTION.Any(a => a == x)).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID)
                }
            );
            RuleForEach(x => x.Assets).SetValidator(
                new InlineValidator<AssetAttributeSeries> {
                    agValidator => agValidator.RuleFor(x => x.TimeGrain)
                                              .Must(x=> PostgresFunction.IsValidGranularity(x)).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID)
                }
            );

            RuleForEach(x => x.Assets).SetValidator(
                new InlineValidator<AssetAttributeSeries> {
                    agValidator => agValidator.RuleFor(x => x.Aggregate)
                                              .Must(x => PostgresFunction.ALLOW_AGGREGATE_FUNCTION.Any(a => a == x.ToLowerInvariant())).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID)
                                              .When(x=> !string.IsNullOrEmpty(x.TimeGrain))
                }
            );
        }
    }
}