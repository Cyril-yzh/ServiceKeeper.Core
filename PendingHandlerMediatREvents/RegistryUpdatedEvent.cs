using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.PendingHandlerMediatREvents
{
    /// <summary>
    /// 注册表更新通知处理端进行处理
    /// 事件处理程序未被实现，需要每个引用方自行实现任务的解析和处理
    /// </summary>
    public record RegistryUpdatedEvent() : INotification;
    /// <summary>
    /// 实现此接口来对 TaskReceivedEvent 进行处理
    /// </summary>
    public interface IRegistryUpdatedEventHandler : INotificationHandler<RegistryUpdatedEvent> { }
}
