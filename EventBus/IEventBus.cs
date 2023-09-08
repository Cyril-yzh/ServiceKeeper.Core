using ServiceKeeper.Core.Entity;

namespace ServiceKeeper.Core.EventBus
{
    public interface IEventBus
    {
        void Publish(string eventName, TaskDetail taskEntity);

        internal void Reply(string eventName, MqReply reply);

        void Subscribe(string eventName, Type handlerType);

        void Unsubscribe(string eventName, Type handlerType);
    }
}
