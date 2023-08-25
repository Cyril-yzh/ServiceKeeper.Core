using ServiceKeeper.Core.EventBus;
using ServiceKeeper.Core.Entity;
using ServiceKeeper.Core.PendingHandlerMediatREvents;
using ServiceKeeper.Core.EventBus.EventHandler;
using MediatR;
using System.Text.Json;

namespace ServiceKeeper.Core.MQEventHandlers
{
    /// <summary>
    /// 通过 EventBus 接收任务 ,再使用 MediatR 发布至外部的任务处理器中
    /// 会使用 ServiceMetadata.MQSubscriptionKey 作为此EventHandler的侦听key
    /// </summary>
    internal class ConsumerReceiveTaskEventHandler : IntegrationEventHandler
    {
        public ConsumerReceiveTaskEventHandler(IEventBus eventBus, IMediator mediator, ServiceRegistry registry) : base(eventBus)
        {
            this.eventBus = eventBus;
            this.mediator = mediator;
            this.registry = registry;
        }
        private readonly IEventBus eventBus;
        private readonly IMediator mediator;
        private readonly ServiceRegistry registry;

        public override async Task Handle(string eventName, string? eventData)
        {
            try
            {
                if (eventData != null)
                {
                    TaskDetail? detail = JsonSerializer.Deserialize<TaskDetail>(eventData);
                    if (detail != null)
                    {
                        bool state = await mediator.Send(new TaskReceivedEvent(detail.Task)); //发给处理端处理并返回结果
                        if (state && detail.NextSuccessTask.HasValue)
                        {
                            ServiceMetadata? producer = registry.Registry.Values.Where(m => m.ServiceRole == ServiceRole.Producer && m.ServiceStatus == ServiceStatus.Active).FirstOrDefault();
                            if (producer != null)
                            {
                                JsonSerializerOptions options = new() { WriteIndented = true };
                                var replyObject = new { Status = MQExcuteStatus.Ok.ToString(), Guid = detail.NextSuccessTask.ToString() };
                                string serializedReply = JsonSerializer.Serialize(replyObject, options);

                                var taskDetail = new TaskDetail
                                {
                                    Key = producer.AssemblyName,
                                    Task = serializedReply
                                };
                                eventBus.Publish(taskDetail);
                            }
                        }
                        if (!state && detail.NextFailureTask.HasValue)
                        {
                            ServiceMetadata? producer = registry.Registry.Values.Where(m => m.ServiceRole == ServiceRole.Producer && m.ServiceStatus == ServiceStatus.Active).FirstOrDefault();
                            if (producer != null)
                            {
                                JsonSerializerOptions options = new() { WriteIndented = true };
                                var replyObject = new { Status = MQExcuteStatus.Failure.ToString(), Guid = detail.NextFailureTask.ToString() };
                                string serializedReply = JsonSerializer.Serialize(replyObject, options);

                                var taskDetail = new TaskDetail
                                {
                                    Key = producer.AssemblyName,
                                    Task = serializedReply
                                };
                                eventBus.Publish(taskDetail);
                            }
                        }
                    }

                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ConsumerReceiveTaskEventHandler 接收任务处理时发生错误:{ex.Message}");
                throw;
            }
        }
    }
}
