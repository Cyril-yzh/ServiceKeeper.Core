namespace ServiceKeeper.Core.EventBus.EventHandler
{
    public abstract class IntegrationEventHandler : IIntegrationEventHandler
    {
        protected readonly IEventBus _eventBus;
        /// <summary>
        /// 需要侦听的Key
        /// </summary>
        public string? EventName { get; private set; }
        public IntegrationEventHandler(IEventBus eventBus)
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
        //因为消息可能会重复发送，因此Handle内的实现需要是幂等的
        public abstract Task Handle(string eventName, string eventData);
    }
}
