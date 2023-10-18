using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core
{
    /// <summary>
    /// 在特定的时间触发
    /// </summary>
    public class TimeSpecific
    {
        public TimeSpecific() { }
        /// <summary>
        /// 输入月份，范围为1-12, 表示在第几个月发送
        /// </summary>
        public List<int>? MonthofYear { get; init; } 
        /// <summary>
        /// 输入日期，范围为1-31, 表示在每月某日发送
        /// </summary>
        public List<int>? DayOfMonth { get; init; }
        /// <summary>
        /// 输入星期几，范围为0-6, 表示在每周某日发送
        /// </summary>
        public List<int>? DayOfWeek { get; set; }
        /// <summary>
        /// 输入时间,范围为0:00-23:59,表示在每日某时间发送, 为空表示每天只在0点发送一次
        /// </summary>
        public List<string>? Times { get; init; }

        public bool ShouldTrigger(DateTime now)
        {
            if (MonthofYear != null && MonthofYear.Contains(now.Month) && DayOfMonth != null && DayOfMonth.Contains(now.Day) && Times != null && Times.Contains(now.ToString("HH:mm")))
            {
                return true;
            }
            if (DayOfWeek != null && DayOfWeek.Contains((int)now.DayOfWeek) && Times != null && Times.Contains(now.ToString("HH:mm")))
            {
                return true;
            }
            return false;
        }
    }
}
