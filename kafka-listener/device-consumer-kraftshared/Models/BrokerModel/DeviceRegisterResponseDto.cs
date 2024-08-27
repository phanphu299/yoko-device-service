namespace Device.Consumer.KraftShared.Model
{
    public class DeviceRegisterResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public DeviceIotAuthenticationResponseDto Payload { get; set; }
    }

    public class DeviceIotAuthenticationResponseDto
    {
        public AuthenticationResponse Authentication { get; set; }
    }

    public class AuthenticationResponse
    {
        public SymmetricKey SymmetricKey { get; set; }
        public X509Thumbprint X509Thumbprint { get; set; }
    }

    public class SymmetricKey
    {
        public string PrimaryKey { get; set; }
        public string SecondaryKey { get; set; }
    }

    public class X509Thumbprint
    {
        public string PrimaryThumbprint { get; set; }
        public string SecondaryThumbprint { get; set; }
    }
}