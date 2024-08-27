using System.Threading;
using System.Threading.Tasks;
using Device.Application.Models;

namespace Device.Application.Service.Abstraction
{
    public interface IUserService
    {
        Task<UserInfoDto> GetUserInfoAsync(string upn, CancellationToken cancellationToken);
    }
}
