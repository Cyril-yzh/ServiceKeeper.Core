using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.MediatREventHandlers
{
    /// <summary>
    /// 发生异常事件
    /// </summary>
    public record ExceptionOccurredEvent(Exception Excption) : INotification;
    /// <summary>
    /// 接收发生异常事件并传入处理端
    /// </summary>
    public interface IExceptionEventHandler : INotificationHandler<ExceptionOccurredEvent> { }
}
