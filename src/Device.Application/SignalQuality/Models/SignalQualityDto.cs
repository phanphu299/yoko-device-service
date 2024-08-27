namespace Device.Application.SignalQuality.Command.Model
{
    public class SignalQualityDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SignalQualityDto(int id, string name)
        {
            Id = id;
            Name = name;
        }
        public SignalQualityDto()
        {

        }
        public SignalQualityDto(Domain.Entity.DeviceSignalQuality quality)
        {
            Id = quality.Id;
            Name = quality.Name;
        }
    }
}