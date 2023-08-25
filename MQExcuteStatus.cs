using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core
{
    /// <summary>
    /// 表示任务的执行结果
    /// </summary>
    public enum MQExcuteStatus
    {
        /// <summary>
        /// 执行成功
        /// </summary>
        Ok = 0,
        /// <summary>
        /// 执行失败
        /// </summary>
        Failure = 1,
        /// <summary>
        /// 找不到对应处理服务
        /// </summary>
        NotFoundService = 2
    }
}
