using System.Threading.Tasks;

namespace Device.Consumer.KraftShared.Models
{
    public interface ILock
    {
        Task WaitAsync();

        int Release();
    }
}