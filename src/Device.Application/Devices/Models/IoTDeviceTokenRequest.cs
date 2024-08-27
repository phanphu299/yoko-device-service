namespace Device.Application.Model
{
    internal class IoTDeviceTokenRequest
    {
        public int SasTokenDuration { get; set; } = 30;
        public string PrimaryKey { get; set; }
        public string SecondaryKey { get; set; }
    }
}