using System;
using System.Linq.Expressions;

namespace Device.Application.Asset.Command.Model
{
    public class AddAssetDto
    {
        static Func<Domain.Entity.Asset, AddAssetDto> Converter = Projection.Compile();
        public Guid Id { get; set; }
        private static Expression<Func<Domain.Entity.Asset, AddAssetDto>> Projection
        {
            get
            {
                return model => new AddAssetDto
                {
                    Id = model.Id
                };
            }
        }

        public static AddAssetDto Create(Domain.Entity.Asset entity)
        {
            return Converter(entity);
        }
    }
}
