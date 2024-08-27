using System;
using Device.Application.Template.Command.Model;
using MediatR;

namespace Device.Application.Template.Command
{
    public class GetTemplateByID : IRequest<GetTemplateDto>
    {
        public Guid Id { get; set; }
        public bool IncludeDeleted { get; set; } = false;
        public bool Deleted { get; set; } = false;
        public GetTemplateByID(Guid id)
        {
            Id = id;
        }
    }
}
