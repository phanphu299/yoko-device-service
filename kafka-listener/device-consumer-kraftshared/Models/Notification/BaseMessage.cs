namespace Device.Consumer.KraftShared.Model.Notification
{
    public class BaseMessage<T>
    {
        public T Message { get; set; }

        public BaseMessage(T message)
        {
            Message = message;
        }
    }
}