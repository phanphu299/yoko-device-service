using System;
using System.Linq.Expressions;
using Device.Application.Device.Command.Model;
using MediatR;

namespace Device.Application.Device.Command
{
    public class RefreshToken : IRequest<UpdateDeviceDto>
    {
        public RefreshToken(string id)
        {
            Id = id;
        }

        public string Id { get; set; }

        private static Expression<Func<RefreshToken, Domain.Entity.Device>> Projection
        {
            get
            {
                return entity => new Domain.Entity.Device
                {
                    Id = entity.Id,
                };
            }
        }

        public static Domain.Entity.Device Create(RefreshToken command)
        {
            if (command != null)
            {
                return Projection.Compile().Invoke(command);
            }
            return null;
        }
    }
}
