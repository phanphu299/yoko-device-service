namespace Device.Application.Device.Command.Model
{
    public class DeviceRegisterResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public DeviceIotAuthenticationResponseDto Payload { get; set; }
    }
}