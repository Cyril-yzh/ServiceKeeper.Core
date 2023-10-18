using MediatR;
using Microsoft.Extensions.Logging;
using ServiceKeeper.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.MediatR
{
    /// <summary>
    /// 表示 ServiceKeeper 记录了一条日志
    /// 事件处理程序未被实现，需要每个引用方自行实现任务的解析和处理
    /// </summary>
    public record ServiceKeeperLog(LogLevel Level, string Message) : INotification;

    public enum LogLevel
    {
        /// <summary>
        /// 内部控制流和诊断状态转储，以方便查明已识别的问题
        /// </summary>
        Debug,
        /// <summary>
        /// 感兴趣的事件或与外部观察者相关的事件；默认启用的最低日志记录级别
        /// </summary>
        Info,
        /// <summary>
        /// 可能出现问题或服务/功能下降的指标
        /// </summary>    
        Warning,
        /// <summary>
        /// 指示应用程序或连接的系统内出现故障
        /// </summary>
        Error,
        /// <summary>
        /// 导致应用程序完全失败的严重错误
        /// </summary>
        Fatal
    }
}
