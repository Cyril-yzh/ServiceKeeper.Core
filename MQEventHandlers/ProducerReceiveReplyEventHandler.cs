using ServiceKeeper.Core.EventBus;
using ServiceKeeper.Core.EventBus.EventHandler;

namespace ServiceKeeper.Core.MQEventHandlers
{
    /// <summary>
    /// 通过 EventBus 接收任务 ,再使用 MediatR 发布至外部的任务处理器中
    /// 会使用 ServiceMetadata.MQSubscriptionKey 作为此EventHandler的侦听key
    /// </summary>
    internal class ProducerReceiveReplyEventHandler : JsonIntegrationEventHandler<MqReply>
    {
        public ProducerReceiveReplyEventHandler(IEventBus eventBus, ServiceScheduler scheduler) : base(eventBus) { this.scheduler = scheduler; }
        private readonly ServiceScheduler scheduler;

        public override Task HandleJson(string eventName, MqReply? eventData)
        {
            try
            {
                if (eventData != null)
                {
                    _ = eventData.ErrorCode switch
                    {
                        ErrorCode.Success => scheduler.ExcutedTaskCount++,
                        _ => scheduler.FailedTaskCount++,
                    };
                    if (eventData.NextTask.HasValue) scheduler.PublishTask(eventData.NextTask.Value);
                    //TODO:添加对前端和db的日志
                }
                return Task.CompletedTask;
            }
            catch (Exception)
            {
                throw;
            }

        }
    }
}
