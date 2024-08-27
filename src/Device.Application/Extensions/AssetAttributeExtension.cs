using Device.Application.Asset.Command.Model;
using Device.Application.Constant;

namespace Device.Application.Service.Extension
{
    public static class AssetAttributeExtension
    {
        public static bool IsStaticAttribute(this IAssetAttribute attribute)
        {
            return attribute?.AttributeType == AttributeTypeConstants.TYPE_STATIC;
        }

        public static bool IsDynamicAttribute(this IAssetAttribute attribute)
        {
            return attribute?.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC;
        }

        public static bool IsRuntimeAttribute(this IAssetAttribute attribute)
        {
            return attribute?.AttributeType == AttributeTypeConstants.TYPE_RUNTIME;
        }

        public static bool IsAliasAttribute(this IAssetAttribute attribute)
        {
            return attribute?.AttributeType == AttributeTypeConstants.TYPE_ALIAS;
        }

        public static bool IsIntegrationAttribute(this IAssetAttribute attribute)
        {
            return attribute?.AttributeType == AttributeTypeConstants.TYPE_INTEGRATION;
        }

        public static bool IsCommandAttribute(this IAssetAttribute attribute)
        {
            return attribute?.AttributeType == AttributeTypeConstants.TYPE_COMMAND;
        }
    }
}