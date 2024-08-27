using System;
using System.Linq.Expressions;

namespace Device.Application.Asset.Command.Model
{
    public class UpdateAssetDto
    {
        private static Func<Domain.Entity.Asset, UpdateAssetDto> Converter = Projection.Compile();
        public Guid Id { get; set; }
        private static Expression<Func<Domain.Entity.Asset, UpdateAssetDto>> Projection
        {
            get
            {
                return model => new UpdateAssetDto
                {
                    Id = model.Id
                };
            }
        }

        public static UpdateAssetDto Create(Domain.Entity.Asset model)
        {
            return Converter(model);
        }
    }
}
