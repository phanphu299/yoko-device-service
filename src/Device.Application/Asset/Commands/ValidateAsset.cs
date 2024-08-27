using System;
using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class ValidateAsset : IRequest<ValidateAssetResponse>
    {
        public Guid Id { get; set; }

        public ValidateAsset(Guid id)
        {
            Id = id;
        }
    }
}
