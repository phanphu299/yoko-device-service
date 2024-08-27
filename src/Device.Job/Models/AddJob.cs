using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Device.Job.Constant;
using MediatR;

namespace Device.Job.Model
{
    public class AddJob : IRequest<JobDto>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Type { get; set; }
        public string OutputType { get; set; }
        public string FolderPath { get; set; }
        public IDictionary<string, object> Payload { get; set; }

        static Func<AddJob, JobInfo> Converter = Projection.Compile();

        private static Expression<Func<AddJob, JobInfo>> Projection
        {
            get
            {
                return command => new JobInfo
                {
                    Id = command.Id,
                    Type = command.Type.ToLower(),
                    OutputType = command.OutputType.ToLower(),
                    FolderPath = command.FolderPath,
                    Status = JobStatus.PROCESSING,
                    Payload = command.Payload,
                    CreatedUtc = DateTime.UtcNow
                };
            }
        }

        public static JobInfo Create(AddJob command)
        {
            if (command != null)
            {
                return Converter(command);
            }
            return null;
        }
    }
}
