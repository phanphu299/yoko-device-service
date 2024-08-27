using System;
using System.Linq.Expressions;
using Device.Application.Constants;
using Device.Application.TemplateDetail.Command.Model;
using MediatR;
using AHI.Infrastructure.Validation.CustomAttribute;

namespace Device.Application.TemplateDetail.Command
{
    public class AddTemplateDetails : IRequest<AddTemplateDetailsDto>, INotification
    {
        public int TemplatePayloadId { get; set; }
        [DynamicValidation(RemoteValidationKeys.metric)]
        public string Key { get; set; }
        public string Name { get; set; }
        public int KeyTypeId { get; set; }
        public string DataType { get; set; }
        //public int? UomId { get; set; }
        [DynamicValidation(RemoteValidationKeys.expression)]
        public string Expression { get; set; }
        public bool Enabled { get; set; }
        public Guid DetailId { get; set; } = Guid.NewGuid();
        static Func<AddTemplateDetails, Domain.Entity.TemplateDetail> Converter = Projection.Compile();
        private static Expression<Func<AddTemplateDetails, Domain.Entity.TemplateDetail>> Projection
        {
            get
            {
                return entity => new Domain.Entity.TemplateDetail
                {

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

        public static Domain.Entity.TemplateDetail Create(AddTemplateDetails model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
