using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.MediatR
{
    /// <summary>
    /// 实现此接口会侦听到'调度器添加任务事件'
    /// 表示 任务调度器(ServiceScheduler) 添加了任务
    /// 以此对 SchedulerTasksUpdatedEvent 进行处理
    /// </summary>
    public interface ISchedulerAddTaskEventHandler : INotificationHandler<SchedulerAddTaskEvent> { }
}
