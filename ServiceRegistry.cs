using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using StackExchange.Redis;
using ServiceKeeper.Core.PendingHandlerMediatREvents;
using System.Text.Json;

namespace ServiceKeeper.Core
{
    public class ServiceRegistry : IDisposable
    {
        private int DbNumber { get; init; }
        private readonly static object registryLock = new();
        //服务注册信息
        private Dictionary<string, ServiceMetadata> registry = new();
        public Dictionary<string, ServiceMetadata> Registry
        {
            get { lock (registryLock) { return registry; } }
            private set { lock (registryLock) { registry = value; } }
        }
        //服务基础数据
        public ServiceMetadata CurrentOptions { get; set; } = null!;
        private readonly IConnectionMultiplexer redis = null!;
        private readonly IMediator mediator = null!;
        private readonly Timer ElectionTimer = null!;
        private readonly Timer RegisterTimer = null!;
        private readonly Timer RegistryTimer = null!;
        //private readonly object _timerLock = new object();

        internal ServiceRegistry(/*IHostApplicationLifetime lifetime,*/IConnectionMultiplexer redis, ServiceMetadata options, IMediator mediator, int databaseNumber = 15)
        {
            this.DbNumber = databaseNumber;
            this.redis = redis;
            if (!redis.IsConnected) return;
            this.CurrentOptions = options;
            this.mediator = mediator;
            //var db = redis.GetDatabase(DbNumber);
            //var value = db.StringGet(CurrentOptions.RedisKey);
            //if (value.HasValue)
            //{
            //    throw new RegistryException($"已注册了相同key:{CurrentOptions.RedisKey} 的对象,检查是否在同一个服务器下存在相同的服务");
            //}
            ElectionTimer = ElectionKeeperAsync();
            RegisterTimer = RegisterKeeperAsync();
            RegistryTimer = UpdateRegistryAsync();
        }

        /// <summary>
        /// 服务竞选
        /// 如果成功会持续更新自身超时状态
        /// 如果失败则会检测成功竞选服务状态并试图竞选
        /// </summary>
        private Timer ElectionKeeperAsync()
        {
            Timer result = new(async (state) =>
            {
                try
                {
                    if (redis.IsConnected)
                    {
                        if (CurrentOptions.ServiceStatus == ServiceStatus.Active)
                        {
                            // 成为主服务时更新服务状态:如果有 Key 则更新超时时间,如果没有则降级为候选
                            if (!await redis.GetDatabase(DbNumber).KeyExpireAsync(CurrentOptions.ElectionKey, CurrentOptions.ExpiryTime))
                                CurrentOptions.ServiceStatus = ServiceStatus.Standby;
                        }
                        else
                        {
                            // 成为候选服务时检查主服务状态
                            var db = redis.GetDatabase(DbNumber);
                            //string? lockedValue = await db.StringGetAsync(CurrentOptions.ElectionKey);
                            //if (lockedValue == null) // 如果没有主服务,则开始竞选
                            //{
                            // 尝试获取互斥锁,获取到则竞选成功
                            if (await db.StringSetAsync(CurrentOptions.ElectionKey, CurrentOptions.HostName, CurrentOptions.ExpiryTime, When.NotExists))
                            {
                                CurrentOptions.ServiceStatus = ServiceStatus.Active;
                            }
                            else
                            {
                                CurrentOptions.ServiceStatus = ServiceStatus.Standby;
                            }

                            //}
                        }
                    }
                    else
                    {
                        Console.WriteLine($"redis断开!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"主服务时更新服务状态报错: {ex.Message}");
                    //mediator.Publish(new ExceptionOccurredEvent(ex));
                    CurrentOptions.ServiceStatus = ServiceStatus.Standby;
                }
            }, null, CurrentOptions.RenewTime, CurrentOptions.RenewTime);
            return result;
        }


        /// <summary>
        /// 服务注册
        /// 如果成功会持续更新自身超时状态
        /// 如果失败则抛出异常
        /// </summary>
        private Timer RegisterKeeperAsync()
        {
            Timer result = new(async (state) =>
            {
                try
                {
                    if (redis.IsConnected)
                    {
                        //if (!redis.GetDatabase(DbNumber).KeyExpire(CurrentOptions.RedisKey, CurrentOptions.ExpiryTime))// 保持 Service 键的存活时间
                        await redis.GetDatabase(DbNumber).StringSetAsync(CurrentOptions.RedisKey, JsonSerializer.Serialize(CurrentOptions), CurrentOptions.ExpiryTime);
                    }
                    else
                    {
                        await Task.Delay(CurrentOptions.RenewTime);
                    }
                }
                catch (RegistryException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"更新超时时间报错: {ex.Message}");
                    //_ = mediator.Publish(new ExceptionOccurredEvent(ex));
                }
            }, null, CurrentOptions.RenewTime, CurrentOptions.RenewTime);
            return result;
        }

        public Timer UpdateRegistryAsync()
        {
            Timer result = new(async (state) =>
            {
                try
                {
                    bool isUpdated = false;
                    var db = redis.GetDatabase(DbNumber);
                    var updatedKeys = new HashSet<string>();
                    await using var keys = db.Multiplexer.GetServer(redis.GetEndPoints().First()).KeysAsync(DbNumber, $"RegistryService.*").GetAsyncEnumerator();
                    while (await keys.MoveNextAsync())
                    {
                        var value = await db.StringGetAsync(keys.Current);
                        ServiceMetadata? options = JsonSerializer.Deserialize<ServiceMetadata>(value.ToString());
                        if (options == null) continue;
                        if (!Registry.ContainsKey(keys.Current.ToString()) || !Registry[keys.Current.ToString()].Equals(options))
                        {
                            Registry[keys.Current.ToString()] = options;
                            isUpdated = true;
                        }
                        updatedKeys.Add(keys.Current.ToString());// 将redis所有key记录
                    }
                    foreach (var removedKey in Registry.Keys.Except(updatedKeys).ToList())// 找到所有不在redis的项
                    {
                        Registry.Remove(removedKey);
                        isUpdated = true;
                    }
                    if (isUpdated) _ = mediator.Publish(new RegistryUpdatedEvent());
                }
                catch (Exception ex)// 处理异常，清空registry重载
                {
                    Console.WriteLine($"获取Redis中注册的所有服务报错: {ex.Message}");
                    registry.Clear();
                    var db = redis.GetDatabase(DbNumber);
                    var keys = db.Multiplexer.GetServer(redis.GetEndPoints().First()).KeysAsync(DbNumber, $"RegistryService.*").GetAsyncEnumerator();
                    while (await keys.MoveNextAsync())
                    {
                        var value = await db.StringGetAsync(keys.Current);
                        ServiceMetadata? options = JsonSerializer.Deserialize<ServiceMetadata>(value.ToString());
                        if (options != null) Registry.Add(keys.Current.ToString(), options);
                    }
                    _ = mediator.Publish(new RegistryUpdatedEvent());
                }
            }, null, CurrentOptions.ExpiryTime, CurrentOptions.ExpiryTime);
            return result;
        }

        public async Task<List<string>> GetRegistryKeysAsync()
        {
            var keysList = new List<string>();
            var db = redis.GetDatabase(DbNumber);
            await using var keys = db.Multiplexer.GetServer(redis.GetEndPoints().First()).KeysAsync(DbNumber, $"RegistryService.*").GetAsyncEnumerator();
            while (await keys.MoveNextAsync())
            {
                var key = await db.StringGetAsync(keys.Current);
                keysList.Add(key.ToString());
            }
            return keysList;
        }


        /// <summary>
        /// 如果已注册为主服务,触发UnElection会取消注册
        /// </summary>
        public void UnElection()
        {
            if (CurrentOptions.ServiceStatus == ServiceStatus.Active)
            {
                var db = redis.GetDatabase(DbNumber);
                db.KeyDelete(CurrentOptions.ElectionKey);
            }
        }

        /// <summary>
        /// 触发Unregister会从注册表中移除
        /// </summary>
        public void Unregister()
        {
            var db = redis.GetDatabase(DbNumber);
            db.KeyDelete(CurrentOptions.RedisKey);
        }


        public void Dispose()
        {
            ElectionTimer.Dispose();
            RegisterTimer.Dispose();
            RegistryTimer.Dispose();

            UnElection();
            Unregister();

            GC.SuppressFinalize(this);
        }
    }
    internal class RegistryException : Exception
    {
        public RegistryException(string message) : base(message) { }
    }
}
