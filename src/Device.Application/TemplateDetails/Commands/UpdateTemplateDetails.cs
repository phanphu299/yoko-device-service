
using System;
using System.Linq.Expressions;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.TemplateDetail.Command
{
    public class UpdateTemplateDetails : IRequest<BaseResponse>
    {
        public int Id { get; set; }
        public int TemplatePayloadId { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public int KeyTypeId { get; set; }
        public string DataType { get; set; }
        //public int? UomId { get; set; }
        public string Expression { get; set; }
        public bool Enabled { get; set; }
        public Guid DetailId { get; set; } = Guid.NewGuid();
        static Func<UpdateTemplateDetails, Domain.Entity.TemplateDetail> Converter = Projection.Compile();
        private static Expression<Func<UpdateTemplateDetails, Domain.Entity.TemplateDetail>> Projection
        {
            get
            {
                return entity => new Domain.Entity.TemplateDetail
                {
                    Id = entity.Id,
                    TemplatePayloadId = entity.TemplatePayloadId,
                    Key = entity.Key,
                    Name = entity.Name,
                    KeyTypeId = entity.KeyTypeId,
                    Expression = entity.Expression,
                    DataType = entity.DataType,
                    //UomId = entity.UomId,
                    Enabled = entity.Enabled,
                    DetailId = entity.DetailId
                };
            }
        }

        public static Domain.Entity.TemplateDetail Create(UpdateTemplateDetails model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
