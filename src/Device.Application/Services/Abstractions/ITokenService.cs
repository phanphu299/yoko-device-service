using System.Threading.Tasks;

namespace Device.Application.Service.Abstraction
{
    public interface ITokenService
    {
        Task<bool> CheckTokenAsync(string token, string prefix = null);
    }
}