
namespace Device.Consumer.KraftShared.Models
{
    public interface ILockFactory
    {
        ILock Create(string key);
    }
}