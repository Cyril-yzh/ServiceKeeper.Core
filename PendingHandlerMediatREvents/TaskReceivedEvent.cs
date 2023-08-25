using MediatR;

namespace ServiceKeeper.Core.PendingHandlerMediatREvents
{
    /// <summary>
    /// 收到任务返回至消费者进行处理
    /// 事件处理程序未被实现，需要每个引用方自行实现任务的解析和处理
    /// </summary>
    public record TaskReceivedEvent(string TaskJson) : IRequest<bool>;
    /// <summary>
    /// 实现此接口来对 TaskReceivedEvent 进行处理
    /// </summary>
    public interface ITaskReceivedEventHandler : IRequestHandler<TaskReceivedEvent, bool> { }
}
