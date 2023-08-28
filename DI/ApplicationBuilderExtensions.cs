using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceKeeper.Core.EventBus;
using ServiceKeeper.Core.EventBus.EventHandler;
using ServiceKeeper.Core.MQEventHandlers;
using ServiceKeeper.Core.PendingHandlerMediatREvents;

namespace ServiceKeeper.Core.DI
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseProducerServiceKeeper(this IApplicationBuilder appBuilder)
        {
            UseEventBus(appBuilder);
            var serviceRegistry = appBuilder.ApplicationServices.GetRequiredService<ServiceRegistry>();
            _ = appBuilder.ApplicationServices.GetRequiredService<ServiceTaskScheduler>();
            ProducerReceiveReplyEventHandler producerReceiverEventHandler = appBuilder.ApplicationServices.GetService<ProducerReceiveReplyEventHandler>() ?? throw new Exception("未注册 ProducerReceiveReplyEventHandler ,无法完成 EventBus 注册");
            producerReceiverEventHandler.SetEventName(serviceRegistry.CurrentOptions.AssemblyName);
            return appBuilder;
        }

        public static IApplicationBuilder UseConsumerServiceKeeper(this IApplicationBuilder appBuilder)
        {
            UseEventBus(appBuilder);
            var serviceRegistry = appBuilder.ApplicationServices.GetRequiredService<ServiceRegistry>();
            _ = appBuilder.ApplicationServices.GetService<ITaskReceivedEventHandler>() ?? throw new Exception("未实现 ITaskReceivedEventHandler,无法完成 ServiceKeeper 注册");
            ConsumerReceiveTaskEventHandler serviceKeeperEventHandler = appBuilder.ApplicationServices.GetService<ConsumerReceiveTaskEventHandler>() ?? throw new Exception("未注册 TaskReceivedEventHandler ,无法完成 EventBus 注册");
            serviceKeeperEventHandler.SetEventName(serviceRegistry.CurrentOptions.AssemblyName);
            return appBuilder;
        }

        private static IApplicationBuilder UseEventBus(this IApplicationBuilder appBuilder)
        {
            var serviceProvider = appBuilder.ApplicationServices;
            //获得IEventBus一次，就会立即加载IEventBus，这样扫描所有的EventHandler，保证消息及时消费
            var eventBus = appBuilder.ApplicationServices.GetService<IEventBus>() ?? throw new ApplicationException("找不到IEventBus实例");
            foreach (var eventHandler in serviceProvider.GetServices<IIntegrationEventHandler>())
            {
                if (typeof(IIntegrationEventHandler).IsAssignableFrom(eventHandler.GetType()))
                {
                    var eventNamesProperty = eventHandler.GetType().GetProperty("EventName");
                    if (eventNamesProperty != null && eventNamesProperty.PropertyType == typeof(string))
                    {
                        if (eventNamesProperty.GetValue(eventHandler) is string eventName && !string.IsNullOrEmpty(eventName))
                            eventBus.Subscribe(eventName, eventHandler.GetType());
                    }
                    else
                    {
                        throw new ApplicationException($"{eventHandler} 中的 “事件名称”属性未声明或不是 string 类型");
                    }
                }
            }
            return appBuilder;
        }
    }
}
