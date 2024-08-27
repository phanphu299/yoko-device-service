using System;
using System.Collections.Generic;

namespace Device.Job.Model
{
    public class JobInfo
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string OutputType { get; set; }
        public string FolderPath { get; set; }
        public string FilePath { get; set; }
        public string Status { get; set; }
        public string FailedMessage { get; set; }
        public IDictionary<string, object> Payload { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}