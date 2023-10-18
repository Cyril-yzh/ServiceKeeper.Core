using MediatR;
using ServiceKeeper.Core.MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.MediatR
{
    /// <summary>
    /// 实现此接口会侦听到'注册表更新事件'
    /// 表示 服务注册表 发生了更新
    /// 以此对 ServiceRegistryUpdatedEvent 进行处理
    /// </summary>
    public interface IServiceRegistryUpdatedEventHandler : INotificationHandler<ServiceRegistryUpdatedEvent> { }
}
