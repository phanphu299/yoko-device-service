using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Device.Application.Repository
{
    public interface IReadAssetAttributeTemplateRepository : IReadRepository<Domain.Entity.AssetAttributeTemplate, Guid>
    {
        Task ValidateRemoveAttributeAsync(Guid attributeId);
    }
}