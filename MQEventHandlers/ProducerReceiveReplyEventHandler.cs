using ServiceKeeper.Core.EventBus;
using ServiceKeeper.Core.EventBus.EventHandler;

namespace ServiceKeeper.Core.MQEventHandlers
{
    /// <summary>
    /// 通过 EventBus 接收任务 ,再使用 MediatR 发布至外部的任务处理器中
    /// 会使用 ServiceMetadata.MQSubscriptionKey 作为此EventHandler的侦听key
    /// </summary>
    internal class ProducerReceiveReplyEventHandler : DynamicIntegrationEventHandler
    {
        public ProducerReceiveReplyEventHandler(IEventBus eventBus, ServiceTaskScheduler scheduler) : base(eventBus) { this.scheduler = scheduler; }
        private readonly ServiceTaskScheduler scheduler;

        public override Task HandleDynamic(string eventName, dynamic eventData)
        {
            try
            {
                if (eventData != null)
                {
                    if (Enum.TryParse<MQExcuteStatus>((string)eventData["Status"], out MQExcuteStatus status))
                    {
                        string guidString = (string)eventData["Guid"];
                        if (!string.IsNullOrEmpty(guidString) && Guid.TryParse(guidString, out Guid guid))
                        {
                            scheduler.PublishTask(guid);
                        }
                        if (status == MQExcuteStatus.Ok) scheduler.ExcutedTaskCount++;
                        if (status == MQExcuteStatus.Failure) scheduler.FailedTaskCount++;
                        if (status == MQExcuteStatus.NotFoundService) scheduler.NotFoundTaskCount++;
                    }
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
