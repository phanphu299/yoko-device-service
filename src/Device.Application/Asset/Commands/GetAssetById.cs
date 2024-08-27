using System;
using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public abstract class BaseGetAsset
    {
        public bool UseCache { get; set; } = true;
        public Guid Id { get; set; }
        public bool AuthorizeUserAccess { get; protected set; } = true;
        public bool AuthorizeAssetAttributeAccess { get; protected set; } = true;
    }
    public class GetAssetById : BaseGetAsset, IRequest<GetAssetDto>
    {
        public GetAssetById(Guid id, bool authorizeUserAccess = true, bool authorizeAssetAttributeAccess = true)
        {
            Id = id;
            AuthorizeUserAccess = authorizeUserAccess;
            AuthorizeAssetAttributeAccess = authorizeAssetAttributeAccess;
        }
    }

    public class GetFullAssetById : BaseGetAsset, IRequest<GetFullAssetDto>
    {
        public GetFullAssetById(Guid id, bool authorizeUserAccess = true, bool authorizeAssetAttributeAccess = true)
        {
            Id = id;
            AuthorizeUserAccess = authorizeUserAccess;
            AuthorizeAssetAttributeAccess = authorizeAssetAttributeAccess;
        }
    }
}
