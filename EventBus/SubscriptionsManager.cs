using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.EventBus
{
    /// <summary>
    /// 管理事件处理的注册(侦听)和事件发分发
    /// </summary>
    internal class SubscriptionsManager
    {
        //key是eventName，值是监听这个事件的实现了IIntegrationEventHandler接口的类型
        private readonly Dictionary<string, Type> _handlers = new();

        public event EventHandler<string>? OnEventRemoved;

        public bool IsEmpty => !_handlers.Keys.Any();
        public void Clear() => _handlers.Clear();

        /// <summary>
        /// 把eventHandlerType类型（实现了eventHandlerType接口）注册为监听了eventName事件
        /// </summary>
        public void AddSubscription(string eventName, Type eventHandlerType)
        {
            if (!HasSubscriptionForEvent(eventName))
            {
                _handlers.Add(eventName, eventHandlerType);
            }
            //如果已经注册过
            else
            {
                _handlers[eventName] = eventHandlerType;
                //throw new ArgumentException($"Handler Type {eventHandlerType}已经注册了 '{eventName}'", nameof(eventHandlerType));
            }
        }

        public void RemoveSubscription(string eventName)
        {
            if (HasSubscriptionForEvent(eventName))
            {
                _handlers.Remove(eventName);
                OnEventRemoved?.Invoke(this, eventName);
            }
        }

        /// <summary>
        /// 得到名字为eventName的监听者
        /// </summary>
        public Type GetHandlerForEvent(string eventName) => _handlers[eventName];

        /// <summary>
        /// 是否有类型监听eventName这个事件
        /// </summary>
        public bool HasSubscriptionForEvent(string eventName) => _handlers.ContainsKey(eventName);

    }
}
