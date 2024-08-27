using System;
using MediatR;

namespace Device.Application.BlockTemplate.Command
{
    public class ValidationBlockContent : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string DesignContent { get; set; }
        public string TriggerContent { get; set; }
    }
}
