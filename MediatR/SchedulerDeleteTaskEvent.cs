using MediatR;
using ServiceKeeper.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.MediatR
{
    /// <summary>
    /// 任务调度器添加任务事件
    /// 事件处理程序未被实现，需要每个引用方自行实现任务的解析和处理
    /// </summary>
    public record SchedulerDeleteTaskEvent(Guid Id) : INotification;
}
