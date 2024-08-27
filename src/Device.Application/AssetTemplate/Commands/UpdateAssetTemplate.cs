using System;
using System.Linq.Expressions;
using AHI.Infrastructure.Service.Tag.Model;
using Device.Application.AssetTemplate.Command.Model;
using Device.Application.Constants;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using AHI.Infrastructure.Validation.CustomAttribute;

namespace Device.Application.AssetTemplate.Command
{
    public class UpdateAssetTemplate : UpsertTagCommand , IRequest<UpdateAssetTemplateDto>
    {
        public Guid Id { get; set; }
        [DynamicValidation(RemoteValidationKeys.name)]
        public string Name { get; set; }
        public string CurrentUserUpn { set; get; }
        public DateTime? CurrentTimestamp { set; get; }
        public JsonPatchDocument Attributes { set; get; }
        static Func<UpdateAssetTemplate, Domain.Entity.AssetTemplate> Converter = Projection.Compile();

        private static Expression<Func<UpdateAssetTemplate, Domain.Entity.AssetTemplate>> Projection
        {
            get
            {
                return model => new Domain.Entity.AssetTemplate
                {
                    Id = model.Id,
                    Name = model.Name
                };
            }
        }

        public static Domain.Entity.AssetTemplate Create(UpdateAssetTemplate model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
