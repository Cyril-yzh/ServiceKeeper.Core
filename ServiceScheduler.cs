using ServiceKeeper.Core.EventBus;
using Timer = System.Threading.Timer;
using MediatR;
using ServiceKeeper.Core.MediatR;
using System.Data;
using System.Threading.Tasks;
using System;
using ServiceKeeper.Core;
using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace ServiceKeeper.Core
{
    /// <summary>
    /// 任务调度器
    /// 会将指定的任务根据指定的规则发送到对应的服务
    /// </summary>
    public class ServiceScheduler
    {
        /// <summary>
        /// 保存的计时器
        /// </summary>
        public Dictionary<Guid, Timer> TaskTimers { get; private set; } = new();
        /// <summary>
        /// 保存的触发时间
        /// </summary>
        private readonly Dictionary<Guid, string> lastTriggerTimes = new();
        /// <summary>
        /// 保存的任务
        /// </summary>
        public Dictionary<Guid, TaskEntity> Tasks { get; private set; } = new();
        /// <summary>
        /// 没有Trigger的嵌套(衍生)任务
        /// </summary>
        public Dictionary<Guid, TaskEntity> NoTriggerTasks { get; private set; } = new();
        private readonly IMediator mediator;
        private readonly IEventBus eventBus;
        private readonly ServiceRegistry serviceRegistry;
        /// <summary>
        /// 总共执行了多少任务
        /// </summary>
        public int TotalExecutedTaskCount { get; internal set; }
        /// <summary>
        /// 总共周期成功了多少任务
        /// </summary>
        public int TotalSuccessTaskCount { get; internal set; }
        /// <summary>
        /// 总共失败了多少任务
        /// </summary>
        public int TotalFailedTaskCount { get; internal set; }
        /// <summary>
        /// 总共没找到处理服务的任务数量
        /// </summary>
        public int TotalNotFoundTaskCount { get; internal set; }
        /// <summary>
        /// 当前周期执行了多少任务
        /// </summary>
        public int TimeRangeExecutedTaskCount
        {
            get { lock (timeRangeExecutedTaskLocker) return _timeRangeExecutedTaskCount; }
            set { lock (timeRangeExecutedTaskLocker) _timeRangeExecutedTaskCount = value; }
        }
        private static readonly object timeRangeExecutedTaskLocker = new();
        private int _timeRangeExecutedTaskCount;
        /// <summary>
        /// 当前周期成功了多少任务
        /// </summary>
        public int TimeRangeSuccessTaskCount
        {
            get { lock (timeRangeSuccessTaskLocker) return _timeRangeSuccessTaskCount; }
            set { lock (timeRangeSuccessTaskLocker) _timeRangeSuccessTaskCount = value; }
        }
        private static readonly object timeRangeSuccessTaskLocker = new();
        private int _timeRangeSuccessTaskCount;
        /// <summary>
        /// 当前周期失败了多少任务
        /// </summary>
        public int TimeRangeFailedTaskCount
        {
            get { lock (timeRangeFailedTaskLocker) return _timeRangeFailedTaskCount; }
            set { lock (timeRangeFailedTaskLocker) _timeRangeFailedTaskCount = value; }
        }
        private static readonly object timeRangeFailedTaskLocker = new();
        private int _timeRangeFailedTaskCount;
        /// <summary>
        /// 当前周期没找到处理服务的任务数量
        /// </summary>
        public int TimeRangeNotFoundTaskCount
        {
            get { lock (timeRangeNotFoundTaskLocker) return _timeRangeNotFoundTaskCount; }
            set { lock (timeRangeNotFoundTaskLocker) _timeRangeNotFoundTaskCount = value; }
        }
        private static readonly object timeRangeNotFoundTaskLocker = new();
        private int _timeRangeNotFoundTaskCount;
        //一共注册了多少任务
        private static readonly object registeredTaskLocker = new();
        private int _registeredTaskCount;
        public int RegisteredTaskCount
        {
            get { lock (registeredTaskLocker) return _registeredTaskCount; }
            set { lock (registeredTaskLocker) _registeredTaskCount = value; }
        }
        public ServiceScheduler(IMediator mediator, IEventBus eventBus, ServiceRegistry serviceRegistry)
        {
            this.mediator = mediator;
            this.eventBus = eventBus;
            this.serviceRegistry = serviceRegistry;
            TimeRangeExecutedTaskCount = 0;
            TimeRangeFailedTaskCount = 0;
            TimeRangeNotFoundTaskCount = 0;
            RegisteredTaskCount = 0;
        }

        /// <summary>
        /// 创建任务计时器
        /// 如果已有相同Id的任务则更新
        /// </summary>
        public void AddTask(TaskEntity task)
        {
            if (Tasks.ContainsKey(task.Detail.Id)) DeleteTask(task.Detail.Id, true);
            if (task.Trigger == null)
            {
                _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Error, $"试图将没有触发器的任务添加至调度器,任务名称:{task.Detail.Name}"));
                return;
            }
            if (task.Trigger.TriggerType == TaskTriggerType.TimeInterval && task.Trigger.TimeInterval == null)
            {
                _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Error, $"选择了时间间隔模式,但找不到任务的时间间隔模式配置,任务名称:{task.Detail.Name}"));
                return;
            }
            if (task.Trigger.TriggerType == TaskTriggerType.SpecificTime && task.Trigger.TimeSpecific == null)
            {
                _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Error, $"选择了策略时间模式,但找不到任务的策略时间模式配置,任务名称:{task.Detail.Name}"));
                return;
            }


            Timer timer = task.Trigger.TriggerType switch
            {
                TaskTriggerType.SpecificTime => PublishTaskTimeSpecific(task),
                TaskTriggerType.TimeInterval => PublishTaskByTimeInterval(task),
                _ => throw new Exception()
            };
            Tasks.Add(task.Detail.Id, task);
            if (task.Trigger.TriggerType == TaskTriggerType.SpecificTime) lastTriggerTimes[task.Detail.Id] = "";
            AddInnerTask(task);
            TaskTimers.Add(task.Detail.Id, timer);
            RegisteredTaskCount++;
            _ = mediator.Publish(new SchedulerAddTaskEvent(task));

        }

        /// <summary>
        /// 添加内部嵌套(衍生)任务
        /// 这些任务不会主动触发,只会在前置任务完成后触发
        /// </summary>
        internal void AddInnerTask(TaskEntity task)
        {
            if (task.NextSuccessTask != null)
            {
                AddInnerTask(task.NextSuccessTask);
            }
            if (task.NextFailureTask != null)
            {
                AddInnerTask(task.NextFailureTask);
            }
            if (task.NextNotFoundTask != null)
            {
                AddInnerTask(task.NextNotFoundTask);
            }
            NoTriggerTasks.Add(task.Detail.Id, task);

        }

        /// <summary>
        /// 删除任务计时器
        /// </summary>
        /// <param name="isPublish">是否需要发布消息</param>
        public void DeleteTask(Guid id, bool isPublish = true)
        {
            if (Tasks.TryGetValue(id, out TaskEntity? task))
            {
                DeleteInnerTask(id);
                Tasks.Remove(task.Detail.Id);
                TaskTimers[task.Detail.Id]?.Dispose();
                TaskTimers.Remove(task.Detail.Id);
                lastTriggerTimes.Remove(task.Detail.Id);
                RegisteredTaskCount--;
                if (isPublish) _ = mediator.Publish(new SchedulerDeleteTaskEvent(task.Detail.Id));
            }
        }


        /// <summary>
        /// 删除内部计时器
        /// </summary>
        private void DeleteInnerTask(Guid id)
        {
            if (NoTriggerTasks.TryGetValue(id, out TaskEntity? task))
            {
                if (task.NextSuccessTask != null) DeleteInnerTask(task.NextSuccessTask.Detail.Id);
                if (task.NextFailureTask != null) DeleteInnerTask(task.NextFailureTask.Detail.Id);
                if (task.NextNotFoundTask != null) DeleteInnerTask(task.NextNotFoundTask.Detail.Id);
                NoTriggerTasks.Remove(id);
            }
        }

        /// <summary>
        /// 清空所有任务
        /// </summary>
        public void ClearTask()
        {
            foreach (var timerEntry in TaskTimers)
                timerEntry.Value.Dispose();
            Tasks.Clear();
            TaskTimers.Clear();
            lastTriggerTimes.Clear();
        }
        /// <summary>
        /// 给对应的服务发送任务
        /// </summary>
        private Timer PublishTaskByTimeInterval(TaskEntity task)
        {

            Timer timer = new((state) =>
            {
                try
                {
                    if (task.Trigger!.ShouldTrigger(DateTime.Now))
                    {
                        TotalExecutedTaskCount++;                                //无论成功与否,此处算执行一次
                        TimeRangeExecutedTaskCount++;
                        ServiceMetadata? data = serviceRegistry.Registry.Values.Where(m => m.AssemblyName == task.PublishKey && m.ServiceStatus == ServiceStatus.Active).FirstOrDefault();
                        if (data != null)
                        {
                            eventBus.Publish(task.PublishKey, task.Detail);     //是否成功通过MediatR回调,此处无状态
                        }
                        else
                        {
                            TotalNotFoundTaskCount++;                           //未找到
                            TimeRangeNotFoundTaskCount++;
                            if (task.NextNotFoundTask != null)
                            {
                                _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务名为:'{task.Detail.Name}'的任务找不到 Key 为'{task.PublishKey}'的任务处理服务!,开始执行内部嵌套任务:'{task.NextNotFoundTask.Detail.Name}'"));
                                PublishTaskNow(task.NextNotFoundTask);
                            }
                            else
                            {
                                _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务名为:'{task.Detail.Name}'的任务找不到 Key 为'{task.PublishKey}'的任务处理服务!"));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    TotalFailedTaskCount++;                                     //报错视为执行失败
                    TimeRangeFailedTaskCount++;
                    if (task.NextFailureTask != null)
                    {
                        _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务调度器-发送任务名为:'{task.Detail.Name}'的任务时抛出异常:'{ex.Message}',开始执行内部嵌套任务:'{task.NextFailureTask.Detail.Name}'"));
                        PublishTaskNow(task.NextFailureTask);
                    }
                    else
                    {
                        _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务调度器-发送任务名为:'{task.Detail.Name}'的任务时抛出异常:'{ex.Message}'"));
                    }
                }
            }, task, TimeSpan.FromSeconds(task.Trigger!.TimeInterval!.IntervalSeconds), TimeSpan.FromSeconds(task.Trigger!.TimeInterval!.IntervalSeconds));
            return timer;
        }
        /// <summary>
        /// 给对应的服务发送任务
        /// </summary>
        private Timer PublishTaskTimeSpecific(TaskEntity task)
        {
            Timer timer = new((state) =>
            {
                try
                {
                    DateTime now = DateTime.Now;
                    string now_str = now.ToString("MM-dd HH:mm");
                    if (lastTriggerTimes.TryGetValue(task.Detail.Id, out var trigger) && trigger != now_str)
                    {
                        if (task.Trigger!.ShouldTrigger(now))
                        {
                            lastTriggerTimes[task.Detail.Id] = now_str;
                            TotalExecutedTaskCount++;                            //无论成功与否,此处算执行一次
                            TimeRangeExecutedTaskCount++;
                            ServiceMetadata? data = serviceRegistry.Registry.Values.Where(m => m.AssemblyName == task.PublishKey && m.ServiceStatus == ServiceStatus.Active).FirstOrDefault();
                            if (data != null)
                            {
                                eventBus.Publish(task.PublishKey, task.Detail); //是否成功通过MediatR回调,此处无状态
                            }
                            else
                            {
                                TotalNotFoundTaskCount++;                           //未找到
                                TimeRangeNotFoundTaskCount++;
                                if (task.NextNotFoundTask != null)
                                {
                                    _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务调度器发送任务名为:'{task.Detail.Name}'的任务时找不到 Key 为'{task.PublishKey}'的任务处理服务!,开始执行内部嵌套任务:'{task.NextNotFoundTask.Detail.Name}'"));
                                    PublishTaskNow(task.NextNotFoundTask);
                                }
                                else
                                {
                                    _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务调度器发送任务名为:'{task.Detail.Name}'的任务时找不到 Key 为'{task.PublishKey}'的任务处理服务!"));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    TotalFailedTaskCount++;                                     //报错视为执行失败
                    TimeRangeFailedTaskCount++;
                    if (task.NextFailureTask != null)
                    {
                        _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务调度器发送任务名为:'{task.Detail.Name}'的任务时抛出异常:'{ex.Message}',开始执行内部嵌套任务:'{task.NextFailureTask.Detail.Name}'"));
                        PublishTaskNow(task.NextFailureTask);
                    }
                    else
                    {
                        _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务调度器发送任务名为:'{task.Detail.Name}'的任务时抛出异常:'{ex.Message}'"));
                    }
                }

            }, task, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(30));
            return timer;
        }

        /// <summary>
        /// 在注册的任务表中找到对应任务,立刻执行
        /// </summary>
        internal void PublishTaskNow(Guid guid)
        {
            TaskEntity? task = null;
            try
            {

                if (NoTriggerTasks.TryGetValue(guid, out task))
                {
                    PublishTaskNow(task);
                }
                else
                {
                    TotalExecutedTaskCount++;                                        //无论成功与否,此处算执行一次
                    TimeRangeExecutedTaskCount++;
                    TotalNotFoundTaskCount++;
                    TimeRangeNotFoundTaskCount++;
                    _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"无法在任务调度器中找到ID为:'{guid}'的任务"));
                }
            }
            catch (Exception ex)
            {

                if (task != null && task.NextFailureTask != null)
                {
                    PublishTaskNow(task.NextFailureTask);
                }
                else
                {
                    TotalExecutedTaskCount++;                                        //无论成功与否,此处算执行一次
                    TimeRangeExecutedTaskCount++;
                    TotalFailedTaskCount++;                                         //报错视为执行失败
                    TimeRangeFailedTaskCount++;
                    _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务调度器发送ID为:'{guid}'的任务时抛出异常:'{ex.Message}'"));
                }
            }
        }
        /// <summary>
        /// 立刻执行任务
        /// </summary>
        public void PublishTaskNow(TaskEntity task)
        {
            try
            {
                TotalExecutedTaskCount++;                                        //无论成功与否,此处算执行一次
                TimeRangeExecutedTaskCount++;
                ServiceMetadata? data = serviceRegistry.Registry.Values.Where(m => m.AssemblyName == task.PublishKey && m.ServiceStatus == ServiceStatus.Active).FirstOrDefault();
                if (data != null)
                {
                    eventBus.Publish(task.PublishKey, task.Detail);             //是否成功通过MediatR回调,此处无状态
                }
                else
                {
                    TotalNotFoundTaskCount++;                           //未找到
                    TimeRangeNotFoundTaskCount++;
                    if (task.NextNotFoundTask != null)
                    {
                        _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务调度器发送任务名为:'{task.Detail.Name}'的任务时找不到 Key 为'{task.PublishKey}'的任务处理服务!,开始执行内部嵌套任务:'{task.NextNotFoundTask.Detail.Name}'"));
                        PublishTaskNow(task.NextNotFoundTask);
                    }
                    else
                    {
                        _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务调度器发送任务名为:'{task.Detail.Name}'的任务时找不到 Key 为'{task.PublishKey}'的任务处理服务!"));
                    }
                }
            }
            catch (Exception ex)
            {
                TotalFailedTaskCount++;                                     //报错视为执行失败
                TimeRangeFailedTaskCount++;
                if (task.NextFailureTask != null)
                {
                    _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务调度器发送任务名为:'{task.Detail.Name}'的任务时抛出异常:'{ex.Message}',开始执行内部嵌套任务:'{task.NextFailureTask.Detail.Name}'"));
                    PublishTaskNow(task.NextFailureTask);
                }
                else
                {
                    _ = mediator.Publish(new ServiceKeeperLog(LogLevel.Warning, $"任务调度器发送任务名为:'{task.Detail.Name}'的任务时抛出异常:'{ex.Message}'"));
                }
            }
        }


    }
}
