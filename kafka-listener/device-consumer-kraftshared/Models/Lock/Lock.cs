using System.Threading;
using System.Threading.Tasks;

namespace Device.Consumer.KraftShared.Models
{
    public class Lock : ILock
    {
        private readonly SemaphoreSlim _semaphore;

        public Lock(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public async Task WaitAsync()
        {
            await _semaphore.WaitAsync();
        }

        public int Release()
        {
            return _semaphore.Release();
        }
    }
}