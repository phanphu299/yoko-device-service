using System;
using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.AssetTemplate.Command
{
    public class ValidateAssetTemplate : IRequest<ValidateAssetResponse>
    {
        public Guid Id { get; set; }

        public ValidateAssetTemplate(Guid id)
        {
            Id = id;
        }
    }
}
