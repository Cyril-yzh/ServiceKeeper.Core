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
    /// 实现此接口会侦听到'ServiceKeeper日志事件'
    /// 表示 ServiceKeeper 记录了一条日志
    /// 以此对 ServiceKeeperLog 进行处理
    /// </summary>
    public interface IServiceKeeperLogEventHandler : INotificationHandler<ServiceKeeperLog> { }
}
