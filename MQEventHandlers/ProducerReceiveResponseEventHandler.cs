using MediatR;
using ServiceKeeper.Core.EventBus;
using ServiceKeeper.Core.EventBus.EventHandler;
using ServiceKeeper.Core.MediatR;
using ServiceKeeper.Core;

namespace ServiceKeeper.Core.MQEventHandlers
{
    /// <summary>
    /// 通过 EventBus 接收任务 ,再使用 MediatR 发布至外部的任务处理器中
    /// 会使用 ServiceMetadata.MQSubscriptionKey 作为此EventHandler的侦听key
    /// </summary>
    internal class ProducerReceiveResponseEventHandler : JsonIntegrationEventHandler<MQResponse>
    {
        public ProducerReceiveResponseEventHandler(IEventBus eventBus, ServiceScheduler scheduler, IMediator mediator) : base(eventBus) { this.scheduler = scheduler; this.mediator = mediator; }
        private readonly ServiceScheduler scheduler;
        private readonly IMediator mediator;

        public override Task HandleJson(string eventName, MQResponse? eventData)
        {
            if (eventData != null)
            {
                try
                {
                    TaskEntity? task;
                    if (eventData.EventResult.Code == Code.Success)
                    {
                        scheduler.TotalSuccessTaskCount++;
                        scheduler.TimeRangeSuccessTaskCount++;

                        if (eventData.Guid.HasValue && scheduler.NoTriggerTasks.TryGetValue(eventData.Guid.Value, out task) && task.NextSuccessTask != null)
                        {
                            _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Info, $"任务:'{eventData.TaskName}'执行完成,任务消息:'{eventData.EventResult.Message}',开始执行内部嵌套任务:'{task.NextSuccessTask.Detail.Name}'"));
                            scheduler.PublishTaskNow(task.NextSuccessTask);
                        }
                        else
                        {
                            _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Info, $"任务:'{eventData.TaskName}'执行完成,任务消息:'{eventData.EventResult.Message}'"));
                        }
                    }
                    else
                    {
                        scheduler.TotalFailedTaskCount++;
                        scheduler.TimeRangeFailedTaskCount++;
                        if (eventData.Guid.HasValue && scheduler.NoTriggerTasks.TryGetValue(eventData.Guid.Value, out task) && task.NextFailureTask != null)
                        {
                            _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务:'{eventData.TaskName}'执行失败,任务消息:'{eventData.EventResult.Message}',开始执行内部嵌套任务:'{task.NextFailureTask.Detail.Name}'"));
                            scheduler.PublishTaskNow(task.NextFailureTask);
                        }
                        else
                        {
                            _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务:'{eventData.TaskName}'执行失败,任务消息:'{eventData.EventResult.Message}'"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Error, $"ServiceKeeper 接收名为:'{eventData.TaskName}'的任务回复时抛出异常!处理情况: '{eventData.EventResult.Code}' ,异常信息:'{ex.Message}'"));
                    throw;
                }
            }
            return Task.CompletedTask;
        }
    }
}
