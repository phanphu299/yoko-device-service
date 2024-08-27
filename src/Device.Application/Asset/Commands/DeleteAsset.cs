using System;
using System.Linq.Expressions;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Asset
{
    public class DeleteAsset : IRequest<BaseResponse>
    {
        public Guid Id { get; set; }
        public bool WithMedia { get; set; } = false;
        public bool WithTable { get; set; } = false;
        public Domain.Entity.Asset Asset { get; set; }
        public DeleteAsset(Guid id)
        {
            Id = id;
        }
        static Func<DeleteAsset, Domain.Entity.Asset> Converter = Projection.Compile();
        private static Expression<Func<DeleteAsset, Domain.Entity.Asset>> Projection
        {
            get
            {
                return entity => new Domain.Entity.Asset
                {
                    Id = entity.Id
                };
            }
        }

        public static Domain.Entity.Asset Create(DeleteAsset model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
