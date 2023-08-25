
using System.Text.Json;

namespace ServiceKeeper.Core.EventBus.EventHandler
{
    public abstract class JsonIntegrationEventHandler<T> : IIntegrationEventHandler
    {
        protected readonly IEventBus _eventBus;

        public string? EventName { get; private set; }
        public JsonIntegrationEventHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }
        public void SetEventName(string value)
        {
            if (value != null && value.Any())
            {
                if (!string.IsNullOrEmpty(EventName))
                    _eventBus.Unsubscribe(EventName, this.GetType());
                EventName = value;
                _eventBus.Subscribe(value, this.GetType());
            }
        }

        public Task Handle(string eventName, string json)
        {
            T? eventData = JsonSerializer.Deserialize<T>(json);
            return HandleJson(eventName, eventData);
        }

        public abstract Task HandleJson(string eventName, T? eventData);
    }
}
