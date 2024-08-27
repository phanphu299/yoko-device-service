using System;
using MediatR;

namespace Device.Job.Model
{
    public class GetJobStatus : IRequest<JobStatusDto>
    {
        public Guid Id { get; set; }

        public GetJobStatus(Guid id)
        {
            Id = id;
        }
    }
}