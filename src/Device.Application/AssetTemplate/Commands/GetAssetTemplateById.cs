using System;
using Device.Application.AssetTemplate.Command.Model;
using MediatR;

namespace Device.Application.AssetTemplate.Command
{
    public class GetAssetTemplateById : IRequest<GetAssetTemplateDto>
    {
        public Guid Id { get; set; }
        public GetAssetTemplateById(Guid id)
        {
            Id = id;
        }
    }
}
