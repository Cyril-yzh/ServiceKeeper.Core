using ServiceKeeper.Core.EventBus;
using Timer = System.Threading.Timer;
using MediatR;
using ServiceKeeper.Core.Entity;
using ServiceKeeper.Core.MediatREventHandlers;

namespace ServiceKeeper.Core
{
    /// <summary>
    /// 任务调度器
    /// 会将指定的任务根据指定的规则发送到对应的服务
    /// </summary>
    public class ServiceTaskScheduler
    {
        //保存的计时器
        static readonly Dictionary<Guid, Timer> timers = new();
        static readonly Dictionary<Guid, string> lastTimes = new();
        //所有不需要计时器的派生任务
        static readonly Dictionary<Guid, TaskDetail> untriggerTask = new();

        private readonly IMediator mediator;
        private readonly IEventBus eventBus;
        private readonly ServiceRegistry serviceRegistry;
        //当前周期执行了多少任务
        private static readonly object excutedTaskLocker = new();
        private int _excutedTaskCount;
        public int ExcutedTaskCount
        {
            get { lock (excutedTaskLocker) return _excutedTaskCount; }
            set { lock (excutedTaskLocker) _excutedTaskCount = value; }
        }
        //当前周期失败了多少任务
        private static readonly object failedTaskLocker = new();
        private int _failedTaskCount;
        public int FailedTaskCount
        {
            get { lock (failedTaskLocker) return _failedTaskCount; }
            set { lock (failedTaskLocker) _failedTaskCount = value; }
        }
        //当前周期没找到处理服务的任务数量
        private static readonly object notFoundTaskLocker = new();
        private int _notFoundTaskCount;
        public int NotFoundTaskCount
        {
            get { lock (notFoundTaskLocker) return _notFoundTaskCount; }
            set { lock (notFoundTaskLocker) _notFoundTaskCount = value; }
        }
        //一共注册了多少任务
        private static readonly object registeredTaskLocker = new();
        private int _registeredTaskCount;
        public int RegisteredTaskCount
        {
            get { lock (registeredTaskLocker) return _registeredTaskCount; }
            set { lock (registeredTaskLocker) _registeredTaskCount = value; }
        }
        public ServiceTaskScheduler(IMediator mediator, IEventBus eventBus, ServiceRegistry serviceRegistry)
        {
            this.mediator = mediator;
            this.eventBus = eventBus;
            this.serviceRegistry = serviceRegistry;
            ExcutedTaskCount = 0;
            FailedTaskCount = 0;
            NotFoundTaskCount = 0;
            RegisteredTaskCount = 0;
        }

        /// <summary>
        /// 创建任务计时器
        /// 如果已有相同Id的任务则更新
        /// </summary>
        public void AddTask(TaskEntity task)
        {
            if (task.IsFirstNode)
            {
                if (timers.ContainsKey(task.Id)) DeleteTask(task);
                Timer timer = task.Trigger!.TriggerType switch
                {
                    TaskTriggerType.SpecificTime => PublishTaskTimeSpecific(task),
                    TaskTriggerType.TimeInterval => PublishTaskByTimeInterval(task),
                    _ => throw new Exception()
                };

                timers.Add(task.Id, timer);
                RegisteredTaskCount++;
            }
            else
            {
                if (untriggerTask.ContainsKey(task.Id)) DeleteUntraggerTask(task.Detail);
                untriggerTask.Add(task.Id, task.Detail);
            }
        }

        public void DeleteTask(TaskEntity task)
        {
            if (timers.ContainsKey(task.Id))
            {
                timers[task.Id]?.Dispose();
                timers.Remove(task.Id);
                lastTimes.Remove(task.Id);
                RegisteredTaskCount--;
            }
            DeleteUntraggerTask(task.Detail);
        }
        public static void ClearTask()
        {
            foreach (var timerEntry in timers)
                timerEntry.Value.Dispose();
            timers.Clear();
            lastTimes.Clear();
            untriggerTask.Clear();
        }
        private void DeleteUntraggerTask(TaskDetail task)
        {
            if (task.NextSuccessTask.HasValue && untriggerTask.ContainsKey(task.NextSuccessTask.Value))
            {
                DeleteUntraggerTask(untriggerTask[task.NextSuccessTask.Value]);
                untriggerTask.Remove(task.NextSuccessTask.Value);
            }
            if (task.NextFailureTask.HasValue && untriggerTask.ContainsKey(task.NextFailureTask.Value))
            {
                DeleteUntraggerTask(untriggerTask[task.NextFailureTask.Value]);
                untriggerTask.Remove(task.NextFailureTask.Value);
            }
            if (task.NextNotFoundTask.HasValue && untriggerTask.ContainsKey(task.NextNotFoundTask.Value))
            {
                DeleteUntraggerTask(untriggerTask[task.NextNotFoundTask.Value]);
                untriggerTask.Remove(task.NextNotFoundTask.Value);
            }
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
                        ServiceMetadata? data = serviceRegistry.Registry.Values.Where(m => m.AssemblyName == task.Detail.Key && m.ServiceStatus == ServiceStatus.Active).FirstOrDefault();
                        if (data != null)
                        {
                            eventBus.Publish(task.Detail);
                        }
                        else
                        {
                            Console.WriteLine($"找不到 Key 为  {task.Detail.Key} 任务处理服务!");
                            _ = mediator.Publish(new ExceptionOccurredEvent(new Exception("报错!")));
                        }
                    }
                }
                catch (Exception)
                {

                    throw;
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
                    if (lastTimes[task.Id] != now_str)
                    {
                        if (task.Trigger!.ShouldTrigger(now))
                        {
                            lastTimes[task.Id] = now_str;
                            ServiceMetadata? data = serviceRegistry.Registry.Values.Where(m => m.AssemblyName == task.Detail.Key && m.ServiceStatus == ServiceStatus.Active).FirstOrDefault();
                            if (data != null)
                            {
                                eventBus.Publish(task.Detail);
                            }
                            else
                            {
                                Console.WriteLine($"找不到 Key 为  {task.Detail.Key} 任务处理服务!");
                                _ = mediator.Publish(new ExceptionOccurredEvent(new Exception("报错!")));
                            }
                        }
                    }
                }
                catch
                {
                    throw;
                }
            }, task, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(30));
            return timer;

        }
        /// <summary>
        /// 立刻执行对应任务
        /// </summary>
        /// <param name="guid"></param>
        internal void PublishTask(Guid guid)
        {
            untriggerTask.TryGetValue(guid, out var task);
            if (task != null)
            {
                eventBus.Publish(task);
                ExcutedTaskCount++;
            }
            else NotFoundTaskCount++;
        }
    }
}
