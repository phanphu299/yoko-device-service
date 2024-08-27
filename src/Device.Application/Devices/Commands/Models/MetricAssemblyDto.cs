namespace Device.Application.Device.Command.Model
{
    public class MetricAssemblyDto
    {
        public byte[] Data { get; }
        public string Name { get; }
        public MetricAssemblyDto(string name, byte[] data)
        {
            Data = data;
            Name = name;
        }
    }
}
