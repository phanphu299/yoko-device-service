using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AHI.Infrastructure.Service.Tag.Model;
using Device.Application.AssetTemplate.Command.Model;
using MediatR;

namespace Device.Application.AssetTemplate.Command
{
    public class AddAssetTemplate : UpsertTagCommand, IRequest<AddAssetTemplateDto>, INotification
    {
        public string Name { get; set; }
        public IEnumerable<AssetTemplateAttribute> Attributes { get; set; } = new List<AssetTemplateAttribute>();
        static Func<AddAssetTemplate, Domain.Entity.AssetTemplate> Converter = Projection.Compile();
        private static Expression<Func<AddAssetTemplate, Domain.Entity.AssetTemplate>> Projection
        {
            get
            {
                return element => new Domain.Entity.AssetTemplate
                {
                    Name = element.Name
                };
            }
        }

        public static Domain.Entity.AssetTemplate Create(AddAssetTemplate model)
        {
            if (model == null)
                return null;
            return Converter(model);
        }
    }
}
