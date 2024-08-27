using System;
using Device.Application.Device.Command.Model;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.Device.Command
{
    public class GetDevicesByTemplateId : BaseCriteria, IRequest<GetDevicesByTemplateIdDto>
    {
        public Guid Id { get; set; }
        public GetDevicesByTemplateId(Guid id)
        {
            Id = id;
        }
    }
}
