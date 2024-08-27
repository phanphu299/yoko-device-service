using System;
using System.Linq.Expressions;

namespace Device.Application.Device.Command.Model
{
    public class GetMetricsByDeviceIdDto
    {
        public int MetricId { get; set; }
        public string MetricKey { get; set; }
        public string MetricName { get; set; }
        public string Expression { get; set; }
        public string DataType { get; set; }
        static Func<Domain.Entity.TemplateDetail, GetMetricsByDeviceIdDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.TemplateDetail, GetMetricsByDeviceIdDto>> Projection
        {
            get
            {
                return entity => new GetMetricsByDeviceIdDto
                {
                    MetricId = entity.Id,
                    MetricName = entity.Name,
                    MetricKey = entity.Key,
                    Expression = entity.Expression,
                    DataType = entity.DataType
                };
            }
        }
        public static GetMetricsByDeviceIdDto Create(Domain.Entity.TemplateDetail entity)
        {
            return Converter(entity);
        }
    }
}