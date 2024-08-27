using System;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Template.Command
{
    public class DeleteTemplates : IRequest<BaseResponse>
    {
        public IEnumerable<Guid> TemplateIds { get; set; }
    }
}
