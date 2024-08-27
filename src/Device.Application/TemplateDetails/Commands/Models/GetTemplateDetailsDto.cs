using System;
using System.Linq.Expressions;
using Device.Application.TemplateKeyType.Command.Model;

namespace Device.Application.TemplateDetail.Command.Model
{
    public class GetTemplateDetailsDto
    {
        public int Id { get; set; }
        public int TemplatePayloadId { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public int KeyTypeId { get; set; }
        public string DataType { get; set; }
        public string Expression { get; set; }
        public bool Enabled { get; set; }
        public Guid DetailId { get; set; } = Guid.NewGuid();
        public GetTemplateKeyTypeDto TemplateKeyType { get; set; }
        static Func<Domain.Entity.TemplateDetail, GetTemplateDetailsDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.TemplateDetail, GetTemplateDetailsDto>> Projection
        {
            get
            {
                return entity => new GetTemplateDetailsDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    TemplatePayloadId = entity.TemplatePayloadId,
                    Key = entity.Key,
                    KeyTypeId = entity.KeyTypeId,
                    DataType = entity.DataType,
                    Expression = entity.Expression,
                    Enabled = entity.Enabled,
                    DetailId = entity.DetailId,
                    TemplateKeyType = GetTemplateKeyTypeDto.Create(entity.TemplateKeyType),
                };
            }
        }
        public static GetTemplateDetailsDto Create(Domain.Entity.TemplateDetail model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }

    }
}
