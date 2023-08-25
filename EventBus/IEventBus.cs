using ServiceKeeper.Core.Entity;

namespace ServiceKeeper.Core.EventBus
{
    public interface IEventBus
    {
        void Publish(TaskDetail taskEntity);

        void Subscribe(string eventName, Type handlerType);

        void Unsubscribe(string eventName, Type handlerType);
    }
}
