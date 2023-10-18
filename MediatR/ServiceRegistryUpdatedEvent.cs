using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.MediatR
{
    /// <summary>
    /// 注册表更新事件
    /// 事件处理程序未被实现，需要每个引用方自行实现任务的解析和处理
    /// </summary>
    public record ServiceRegistryUpdatedEvent() : INotification;
}
