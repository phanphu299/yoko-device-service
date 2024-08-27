using System.Threading;

namespace Device.Consumer.KraftShared.Models
{
    public class SemaphoreSlimLockFactory : ILockFactory
    {
        public ILock Create(string key)
        {
            return new Lock(new SemaphoreSlim(1, 1));
        }
    }
}
