namespace ServiceKeeper.Core
{
    public class ServiceKeeperOptions
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
        /// 服务说明
        /// </summary>
        public string ServiceDescription { get; set; } = "";
        /// <summary>
        /// 服务刷新时间
        /// </summary>
        public int RenewSeconds { get; set; } = 10;
        /// <summary>
        /// redis中选择第几个数据库用于存放服务信息
        /// </summary>
        public int DatabaseNumber { get; set; } = 15;
    }
}
