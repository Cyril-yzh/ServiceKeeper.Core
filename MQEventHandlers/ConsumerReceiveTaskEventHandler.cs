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
    internal class ConsumerReceiveTaskEventHandler : JsonIntegrationEventHandler<TaskDetail>
    {
        public ConsumerReceiveTaskEventHandler(IEventBus eventBus, IMediator mediator, ServiceRegistry registry) : base(eventBus)
        {
            _mediator = mediator;
            _registry = registry;
        }
        private readonly IMediator _mediator;
        private readonly ServiceRegistry _registry;

        public override async Task HandleJson(string eventName, TaskDetail? eventData)
        {
            try
            {
                if (eventData != null)
                {
                    bool state = await _mediator.Send(new TaskReceivedEvent(eventData.Task)); //发给处理端处理并返回结果
                    SendReply(errorCode: state ? ErrorCode.Success : ErrorCode.Failure, null, nextTaskId: state ? eventData.NextSuccessTask : eventData.NextFailureTask);
                }
                else
                {
                    await Console.Out.WriteLineAsync($"序列化类型 '{typeof(TaskDetail)}' 失败或为空");
                    SendReply(ErrorCode.ParseError, $"序列化类型 '{typeof(TaskDetail)}' 失败或为空", null);
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"'{eventName}'处理任务时发生错误:{ex.Message}");
                SendReply(ErrorCode.Failure, $"'{eventName}'处理任务时发生错误:{ex.Message}", null);
            }
        }

        /// <summary>
        /// 向生产者回复处理情况
        /// </summary>
        /// <param name="nextTaskId">是否有下一个任务</param>
        private void SendReply(ErrorCode errorCode, string? errorMsg, Guid? nextTaskId)
        {
            ServiceMetadata? producer = _registry.Registry.Values.Where(m => m.ServiceRole == ServiceRole.Producer && m.ServiceStatus == ServiceStatus.Active).FirstOrDefault();

            if (producer != null)
            {
                MqReply reply = new()
                {
                    ErrorCode = errorCode,
                    ErrorMessage = errorMsg,
                    NextTask = nextTaskId,
                };
                _eventBus.Reply(producer.AssemblyName, reply);
            }
        }
    }
}
