namespace Device.Application.Asset.Command.Model
{
    public class SendConfigurationResultDto
    {
        public bool IsSuccess { get; set; }
        public System.Guid NewRowVersion { get; set; }

        public SendConfigurationResultDto(bool isSuccess, System.Guid newRowVersion)
        {
            IsSuccess = isSuccess;
            NewRowVersion = newRowVersion;
        }
    }
}