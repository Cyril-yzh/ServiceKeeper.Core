using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.Entity
{
    /// <summary>
    /// 任务执行触发器
    /// </summary>
    public class TaskTrigger
    {
        public TaskTrigger() { }
        public TaskTriggerType TriggerType { get; set; }
        public TimeInterval? TimeInterval { get; set; }
        public TimeSpecific? TimeSpecific { get; set; }

        public TaskTrigger(string triggerType, TimeInterval? timeInterval, TimeSpecific? timeSpecific)
        {
            if (Enum.TryParse<TaskTriggerType>(triggerType, out var type))
            {
                TriggerType = type;
                switch (TriggerType)
                {
                    case TaskTriggerType.TimeInterval:
                        TimeInterval = timeInterval;
                        break;
                    case TaskTriggerType.SpecificTime:
                        TimeSpecific = timeSpecific;
                        break;
                    default:
                        throw new ArgumentException("参数异常:未设定的 TaskTriggerType");
                };
            }

        }

        /// <summary>
        /// 为True时触发
        /// </summary>
        public bool ShouldTrigger(DateTime Now)
        {
            return TriggerType switch
            {
                TaskTriggerType.TimeInterval => TimeInterval!.ShouldTrigger(Now),
                TaskTriggerType.SpecificTime => TimeSpecific!.ShouldTrigger(Now),
                _ => throw new ArgumentException("参数异常:未设定的 TaskTriggerType")
            };
        }
    }
}
