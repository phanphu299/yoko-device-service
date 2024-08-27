using System;

namespace Device.Job.Model
{
    public class JobStatusDto
    {
        public Guid Id { get; set; }
        public string Status { get; set; }
        public int CheckIn { get; set; }
        public string CheckEndpoint { get; set; }
        public string DownloadEndpoint { get; set; }
        public string FailedMessage { get; set; }

        public JobStatusDto(Guid id, int checkIn, string checkEndpoint, string status, string downloadEndpoint, string failedMessage)
        {
            Id = id;
            CheckIn = checkIn;
            CheckEndpoint = checkEndpoint;
            Status = status;
            DownloadEndpoint = downloadEndpoint;
            FailedMessage = failedMessage;
        }
    }
}