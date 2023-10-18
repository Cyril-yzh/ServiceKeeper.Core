using MediatR;
using ServiceKeeper.Core;

namespace ServiceKeeper.Core.MediatR
{
    /// <summary>
    /// 收到任务进行处理
    /// 事件处理程序未被实现，需要每个引用方自行实现任务的解析和处理
    /// </summary>
    public record TaskReceivedEvent(string TaskJson) : IRequest<EventResult>;

}
