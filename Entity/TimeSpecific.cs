using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.Entity
{
    /// <summary>
    /// 在特定的时间触发
    /// </summary>
    public class TimeSpecific
    {
        public TimeSpecific() { }
        public Monthly? Monthly { get; set; }
        public Weekly? Weekly { get; set; }
        public Daily? Daily { get; set; }

        public bool ShouldTrigger(DateTime now)
        {
            if (Monthly != null)
                if (Monthly.CheckMonthlyMatch(now, Monthly)) return true;
            if (Weekly != null)
                if (Weekly.CheckWeeklyMatch(now, Weekly)) return true;
            if (Daily != null)
                if (Daily.CheckDailyMatch(now, Daily)) return true;
            return false;
        }
    }

    public class Monthly
    {
        public Monthly() { }
        /// <summary>
        /// 输入月份，范围为1-12, 表示在第几个月发送
        /// </summary>
        public List<int>? MonthofYear { get; init; }
        /// <summary>
        /// 输入日期，范围为1-31, 表示在每月某日发送
        /// </summary>
        public List<int>? DayOfMonth { get; init; }
        /// <summary>
        /// 输入时间,范围为0:00-23:59,表示在每日某时间发送, 为空表示每天只在0点发送一次
        /// </summary>
        public List<string>? Times { get; init; }
        public static bool CheckMonthlyMatch(DateTime now, Monthly monthly)
        {
            if (monthly.MonthofYear != null && !monthly.MonthofYear.Contains(now.Month))
            {
                return false;
            }
            if (monthly.DayOfMonth != null && !monthly.DayOfMonth.Contains(now.Day))
            {
                return false;
            }
            if (monthly.Times != null && !monthly.Times.Contains(now.ToString("HH:mm")))
            {
                return false;
            }
            return true;
        }
    }

    public class Weekly
    {
        public Weekly() { }
        /// <summary>
        /// 输入第几周，范围为1-5, 表示在每月某周发送
        /// </summary>
        public List<int>? WeekofMonth { get; set; }
        /// <summary>
        /// 输入星期几，范围为0-6, 表示在每周某日发送
        /// </summary>
        public List<int>? DayOfWeek { get; set; }
        /// <summary>
        /// 输入时间,范围为0:00-23:59,表示在每日某时间发送
        /// </summary>
        public List<string>? Times { get; set; }
        public static bool CheckWeeklyMatch(DateTime now, Weekly weekly)
        {
            if (weekly.WeekofMonth != null && !weekly.WeekofMonth.Contains((now.Day - 1) / 7 + 1))
            {
                return false;
            }
            if (weekly.DayOfWeek != null && !weekly.DayOfWeek.Contains((int)now.DayOfWeek))
            {
                return false;
            }
            if (weekly.Times != null && !weekly.Times.Contains(now.ToString("HH:mm")))
            {
                return false;
            }
            return true;
        }
    }

    public class Daily
    {
        public Daily() { }
        /// <summary>
        /// 输入星期几，范围为0-6, 表示在每周某日发送
        /// </summary>
        public List<int>? DayOfWeek { get; set; }
        /// <summary>
        /// 输入时间,范围为0:00-23:59,表示在每日某时间发送, 为空表示每天只在0点发送一次
        /// </summary>
        public List<string>? Times { get; set; }
        public static bool CheckDailyMatch(DateTime now, Daily daily)
        {
            if (daily.DayOfWeek != null && !daily.DayOfWeek.Contains((int)now.DayOfWeek))
            {
                return false;
            }
            if (daily.Times != null && !daily.Times.Contains(now.ToString("HH:mm")))
            {
                return false;
            }
            return true;
        }
    }
}
