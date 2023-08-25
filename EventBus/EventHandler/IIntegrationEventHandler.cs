namespace ServiceKeeper.Core.EventBus.EventHandler
{
    public interface IIntegrationEventHandler
    {
        /// <summary>
        /// 需要侦听的Key
        /// </summary>
        string? EventName { get; }
        public string? GetEventName()
        {
            return EventName;
        }
        abstract void SetEventName(string value);

        //因为消息可能会重复发送，因此Handle内的实现需要是幂等的
        Task Handle(string eventName, string eventData);
    }
}
