using Device.Application.Asset.Command.Model;

namespace Device.Application.BlockFunction.Model
{
    public class AttributeQueryResult : IAssetAttribute
    {
        public string AttributeType { get; set; }
        public BlockQueryResult Result { get; set; }

        public AttributeQueryResult(string attributeType, BlockQueryResult result)
        {
            AttributeType = attributeType;
            Result = result;
        }
    }
}