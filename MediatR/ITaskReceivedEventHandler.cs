using MediatR;
using ServiceKeeper.Core.MediatR;
using ServiceKeeper.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.MediatR
{
    /// <summary>
    /// 实现此接口会侦听到'任务接收事件'
    /// 表示 从的消息队列(RabbitMQEventBus) 获取了一个任务
    /// 以此对 TaskReceivedEvent 进行处理
    /// 在处理结束后返回 bool 表示是否成功
    /// </summary>
    public interface ITaskReceivedEventHandler : IRequestHandler<TaskReceivedEvent, EventResult> { }
}
