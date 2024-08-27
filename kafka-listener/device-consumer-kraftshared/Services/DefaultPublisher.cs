// using System.Threading.Tasks;
// using Device.Consumer.KraftShared.Service.Abstraction;
// using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
//TODO: Enable again once AHI library upgrade to .NET 8
// namespace Device.Consumer.KraftShared.Service
// {
//     public class DefaultPublisher : IPublisher
//     {
//         protected readonly IDomainEventDispatcher _domainEventDispatcher;

//         public DefaultPublisher(IDomainEventDispatcher domainEventDispatcher)
//         {
//             _domainEventDispatcher = domainEventDispatcher;
//         }

//         public Task SendAsync<T>(T message) where T : BusEvent
//         {
//             return _domainEventDispatcher.SendAsync(message);
//         }

//         public Task SendAsync<T>(T message, string topicName) where T : BusEvent
//         {
//             return _domainEventDispatcher.SendAsync(message);
//         }
//     }
// }