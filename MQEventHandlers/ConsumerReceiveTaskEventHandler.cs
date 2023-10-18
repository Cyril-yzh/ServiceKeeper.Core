using ServiceKeeper.Core.EventBus;
using ServiceKeeper.Core.MediatR;
using ServiceKeeper.Core.EventBus.EventHandler;
using MediatR;
using System.Text.Json;
using ServiceKeeper.Core;

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
                    EventResult eventResult = await _mediator.Send(new TaskReceivedEvent(eventData.Task)); //发给处理端处理并返回结果
                    Response(eventData.Name, eventResult, eventData.Id);
                }
                else
                {
                    EventResult eventResult = new(Code.ParseError, $"'{eventName}'序列化类型 '{typeof(TaskDetail)}' 失败或为空");
                    Response(null, eventResult, eventData!.Id);
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                EventResult eventResult = new(Code.Failure, $"'{eventName}'处理任务时发生错误:{ex.Message}");
                Response(null, eventResult, eventData!.Id);
            }
        }

        /// <summary>
        /// 向生产者回复处理情况
        /// </summary>
        private void Response(string? taskName, EventResult eventResult, Guid? nextId)
        {
            ServiceMetadata? producer = _registry.Registry.Values.Where(m => m.ServiceRole == ServiceRole.Producer && m.ServiceStatus == ServiceStatus.Active).FirstOrDefault();

            if (producer != null)
            {
                MQResponse reply = new(taskName, nextId, eventResult);

                _eventBus.Reply(producer.AssemblyName, reply);
            }
        }
    }
}
