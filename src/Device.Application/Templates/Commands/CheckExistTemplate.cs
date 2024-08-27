using System;
using System.Collections.Generic;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.Template.Command
{
    public class CheckExistTemplate : IRequest<BaseResponse>
    {
        public IEnumerable<Guid> Ids { get; set; }

        public CheckExistTemplate(IEnumerable<Guid> ids)
        {
            Ids = ids;
        }
    }
}
