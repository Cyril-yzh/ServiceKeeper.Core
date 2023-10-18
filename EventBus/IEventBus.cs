using ServiceKeeper.Core;
namespace ServiceKeeper.Core.EventBus
{
    public interface IEventBus
    {
        void Publish(string eventName, TaskDetail taskEntity);

        internal void Reply(string eventName, MQResponse reply);

        void Subscribe(string eventName, Type handlerType);

        void Unsubscribe(string eventName, Type handlerType);
    }
}
