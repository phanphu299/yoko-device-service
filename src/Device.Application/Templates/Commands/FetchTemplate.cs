using System;
using Device.Application.Template.Command.Model;
using MediatR;

namespace Device.Application.Template.Command
{
    public class FetchTemplate : IRequest<GetTemplateDto>
    {
        public Guid Id { get; set; }

        public FetchTemplate(Guid id)
        {
            Id = id;
        }
    }
}