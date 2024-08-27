namespace Device.Application.Asset.Command.Model
{
    public class AttributeAssemblyDto
    {
        public byte[] Data { get; }
        public string Name { get; }
        public AttributeAssemblyDto(string name, byte[] data)
        {
            Data = data;
            Name = name;
        }
    }
}