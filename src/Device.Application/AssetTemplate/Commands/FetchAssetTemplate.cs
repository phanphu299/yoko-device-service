using System;
using Device.Application.AssetTemplate.Command.Model;
using MediatR;

namespace Device.Application.AssetTemplate.Command
{
    public class FetchAssetTemplate : IRequest<GetAssetTemplateDto>
    {
        public Guid Id { get; set; }

        public FetchAssetTemplate(Guid id)
        {
            Id = id;
        }
    }
}