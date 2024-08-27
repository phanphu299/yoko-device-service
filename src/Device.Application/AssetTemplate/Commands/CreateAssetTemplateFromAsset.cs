using System;
using Device.Application.AssetTemplate.Command.Model;
using MediatR;

namespace Device.Application.AssetTemplate.Command
{
    public class CreateAssetTemplateFromAsset : IRequest<AddAssetTemplateDto>
    {
        public Guid Id { get; set; }
        public CreateAssetTemplateFromAsset(Guid id)
        {
            Id = id;
        }

    }
}
