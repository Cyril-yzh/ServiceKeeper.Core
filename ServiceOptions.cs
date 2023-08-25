namespace ServiceKeeper.Core
{
    public class ServiceOptions
    {
        /// <summary>
        /// MQ所处主机名
        /// </summary>
        public string MQHostName { get; set; } = "";
        /// <summary>
        /// MQ交换机名称
        /// </summary>
        public string MQExchangeName { get; set; } = "";
        /// <summary>
        /// MQ账号
        /// </summary>
        public string? MQUserName { get; set; }
        /// <summary>
        /// MQ密码
        /// </summary>
        public string? MQPassword { get; set; }
        /// <summary>
        /// MQ队列名
        /// </summary>
        public string MQQueueName { get; set; } = "";
        /// <summary>
        /// 服务说明
        /// </summary>
        public string ServiceDescription { get; set; } = "";
        /// <summary>
        /// 服务超时时间
        /// </summary>
        public int ExpirySeconds { get; set; } = 30;
        /// <summary>
        /// 服务刷新时间
        /// </summary>
        public int RenewSeconds { get; set; } = 10;
    }
}
