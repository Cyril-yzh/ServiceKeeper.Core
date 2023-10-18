using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core
{
    /// <summary>
    /// 任务触发的类型
    /// </summary>
    public enum TaskTriggerType
    {
        TimeInterval,   // 每隔一段设定的时间触发
        SpecificTime    // 在指定的时间触发
    }
}
