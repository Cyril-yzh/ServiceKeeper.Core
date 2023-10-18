using ServiceKeeper.Core;
using ServiceKeeper.Core.ReflectionSerializer;
using System.Text.Json.Serialization;

namespace ServiceKeeper.Core
{
    public class ServiceMetadata
    {
        /// <summary>
        /// 服务注册的选择,这些选项不应该在编译期后被外部修改
        /// ServiceId 和 AssemblyName 作为双主键来保证唯一性
        /// </summary>
        public ServiceMetadata(string description, ServiceRole serviceRole, string mainAsseblyName, Type? receiverTaskType, int expirySeconds = 30, int renewSeconds = 10)
        {
            if (renewSeconds * 2 > expirySeconds) throw new ArgumentException("过期秒数至少是更新秒数的两倍");
            Description = description;
            ServiceRole = serviceRole;
            HostName = System.Net.Dns.GetHostName();
            AssemblyName = mainAsseblyName;
            ServiceStatus = ServiceStatus.Standby;
            ExpiryTime = TimeSpan.FromSeconds(expirySeconds);
            RenewTime = TimeSpan.FromSeconds(renewSeconds);
            ElectionKey = $"ElectionService.{AssemblyName}";
            RedisKey = $"RegistryService.{AssemblyName} Host:{HostName}";
            if (receiverTaskType != null)
            {
                JsonGenerator jsonGenerator = new();
                ReflectionJson = jsonGenerator.GenerateJson(receiverTaskType);
            }
        }
        [JsonConstructor]
        public ServiceMetadata(string description, ServiceRole serviceRole, string assemblyName, string hostName, string redisKey, ServiceStatus serviceStatus, TimeSpan expiryTime, TimeSpan renewTime, string electionKey, string reflectionJson)
        {
            Description = description;
            ServiceRole = serviceRole;
            AssemblyName = assemblyName;
            HostName = hostName;
            RedisKey = redisKey;
            ServiceStatus = serviceStatus;
            ExpiryTime = expiryTime;
            RenewTime = renewTime;
            ElectionKey = electionKey;
            ReflectionJson = reflectionJson;
        }
        private readonly static object statusLock = new();
        private ServiceStatus serviceStatus;

        /// <summary>
        /// 服务的功能说明
        /// </summary>
        public string Description { get; init; }
        /// <summary>
        /// 主程序集的名称,用于区分不同服务
        /// 作为 MQ 发送/侦听消息 的 Key
        /// </summary>
        public string AssemblyName { get; init; }
        /// <summary>
        /// 服务器的主机名,用于区分不同服务器
        /// </summary>
        public string HostName { get; init; }
        /// <summary>
        /// 唯一id,并作为注册至 Redis 的Key
        /// </summary>
        public string RedisKey { get; init; }
        /// <summary>
        /// 服务的状态:主/备
        /// </summary>
        public ServiceStatus ServiceStatus
        {
            get
            {
                lock (statusLock)
                {
                    return serviceStatus;
                }
            }
            set
            {
                lock (statusLock)
                {
                    serviceStatus = value;
                }
            }
        }
        /// <summary>
        /// 服务的角色:生产者/消费者
        /// </summary>
        public ServiceRole ServiceRole { get; init; }
        /// <summary>
        /// 服务注册超时时间
        /// </summary>
        public TimeSpan ExpiryTime { get; init; }
        /// <summary>
        /// 服务刷新注册时间
        /// </summary>
        public TimeSpan RenewTime { get; init; }
        /// <summary>
        /// 相同Id竞选主服务使用的Key
        /// </summary>
        public string ElectionKey { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        /// <summary>
        /// 通过反射获取的Json
        /// </summary>
        public string? ReflectionJson { get; init; }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            if (obj is not ServiceMetadata other) return false;

            return Description == other.Description &&
                   AssemblyName == other.AssemblyName &&
                   HostName == other.HostName &&
                   RedisKey == other.RedisKey &&
                   ServiceStatus == other.ServiceStatus &&
                   ServiceRole == other.ServiceRole &&
                   ExpiryTime == other.ExpiryTime &&
                   RenewTime == other.RenewTime &&
                   ElectionKey == other.ElectionKey &&
                   ReflectionJson == other.ReflectionJson;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Description.GetHashCode();
                hash = hash * 23 + AssemblyName.GetHashCode();
                hash = hash * 23 + HostName.GetHashCode();
                hash = hash * 23 + RedisKey.GetHashCode();
                hash = hash * 23 + ServiceStatus.GetHashCode();
                hash = hash * 23 + ServiceRole.GetHashCode();
                hash = hash * 23 + ExpiryTime.GetHashCode();
                hash = hash * 23 + RenewTime.GetHashCode();
                hash = hash * 23 + ElectionKey.GetHashCode();
                if (ReflectionJson != null)
                {
                    hash = hash * 23 + ReflectionJson.GetHashCode();
                }
                return hash;
            }
        }

    }
}