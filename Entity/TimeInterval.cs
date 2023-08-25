using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.Entity
{
    /// <summary>
    /// 每隔一段时间触发
    /// 支持在某一部分时间不触发
    /// </summary>
    public class TimeInterval
    {
        public TimeInterval() { }
        public TimeInterval(int intervalSeconds)
        {
            IntervalSeconds = intervalSeconds;
        }

        public TimeInterval(int intervalSeconds, List<TimeSpanRange>? nonTriggerTimeRanges)
        {
            IntervalSeconds = intervalSeconds;
            NonTriggerTimeRanges = nonTriggerTimeRanges;
        }

        /// <summary>
        /// 间隔秒数
        /// </summary>
        public int IntervalSeconds { get; set; }
        /// <summary>
        /// 非触发时间范围
        /// </summary>
        public List<TimeSpanRange>? NonTriggerTimeRanges { get; set; }

        public bool ShouldTrigger(DateTime now)
        {
            if (NonTriggerTimeRanges != null)
            {
                var currentTimeOfDay = now.TimeOfDay;

                foreach (var range in NonTriggerTimeRanges)
                {
                    if (currentTimeOfDay >= range.StartTime && currentTimeOfDay <= range.EndTime)
                    {
                        return false;
                    }
                }
            }

            // 其他触发判断逻辑...

            return true;
        }
    }

    /// <summary>
    /// 时间范围
    /// </summary>
    public class TimeSpanRange
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
