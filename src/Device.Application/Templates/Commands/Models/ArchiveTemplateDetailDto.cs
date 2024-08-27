using System;
using System.Linq.Expressions;

namespace Device.Application.Template.Command.Model
{
    public class ArchiveTemplateDetailDto
    {
        public int Id { get; set; }
        public int? TemplatePayloadId { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public int? KeyTypeId { get; set; }
        public string DataType { get; set; }
        public string Expression { get; set; }
        public string ExpressionCompile { get; set; }
        public bool Enabled { get; set; }
        public Guid? DetailId { get; set; }
        static Func<Domain.Entity.TemplateDetail, ArchiveTemplateDetailDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.TemplateDetail, ArchiveTemplateDetailDto>> Projection
        {
            get
            {
                return entity => new ArchiveTemplateDetailDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    TemplatePayloadId = entity.TemplatePayloadId,
                    Key = entity.Key,
                    KeyTypeId = entity.KeyTypeId,
                    DataType = entity.DataType,
                    Expression = entity.Expression,
                    ExpressionCompile = entity.ExpressionCompile,
                    Enabled = entity.Enabled,
                    DetailId = entity.DetailId
                };
            }
        }
        public static ArchiveTemplateDetailDto Create(Domain.Entity.TemplateDetail model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
