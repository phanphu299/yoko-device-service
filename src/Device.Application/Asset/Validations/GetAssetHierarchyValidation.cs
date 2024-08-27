using Device.Application.Asset.Command;
using FluentValidation;

namespace Device.Application.Asset.Validation
{
    public class GetAssetHierarchyValidation : AbstractValidator<GetAssetHierarchy>
    {
        public GetAssetHierarchyValidation()
        {
            RuleFor(x => x.AssetName).MaximumLength(255);
        }
    }
}
