using System;
using System.Threading.Tasks;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
    public interface IPublisher
    {
        Task SendAsync<T>(T message,string topicName) where T : class;

        Task SendAsync<T>(T message,string topicName, string key) where T : class;
    }
}
