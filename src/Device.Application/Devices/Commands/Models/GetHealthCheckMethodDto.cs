using System;
using System.Linq.Expressions;

namespace Device.Application.Device.Command.Model
{
    public class GetHealthCheckMethodDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        private static Expression<Func<Domain.Entity.HealthCheckMethod, GetHealthCheckMethodDto>> Projection
        {
            get
            {
                return entity => new GetHealthCheckMethodDto
                {
                    Id = entity.Id,
                    Name = entity.Name
                };
            }
        }

        public static GetHealthCheckMethodDto Create(Domain.Entity.HealthCheckMethod entity)
        {
            if (entity == null)
                return null;
            return Projection.Compile().Invoke(entity);
        }
    }
}
