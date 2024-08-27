using System;
using System.Collections.Generic;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.TemplateDetail.Command
{
    public class ValidationTemplateDetails : IRequest<BaseResponse>
    {
        public ICollection<string> Keys { get; set; }
        public Guid Id { get; set; }
    }
}
