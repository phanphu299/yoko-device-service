using System;

namespace Device.Job.Model
{
    public class JobDto
    {
        public Guid Id { get; set; }
        public int CheckIn { get; set; }
        public string CheckEndpoint { get; set; }

        public JobDto(Guid id, int checkIn, string checkEndpoint)
        {
            Id = id;
            CheckIn = checkIn;
            CheckEndpoint = checkEndpoint;
        }
    }
}