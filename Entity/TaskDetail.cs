namespace ServiceKeeper.Core.Entity
{
    /// <summary>
    /// 任务详情
    /// 其中的 TaskJson 是具体的任务,需要处理方自行解析
    /// </summary>
    public class TaskDetail
    {
        //public TaskDetail() { }
        //public TaskDetail(string key, string task)
        //{
        //    Key = key;
        //    Task = task;
        //}
        /// <summary>
        /// 发送至处理服务的Key
        /// </summary>
        public string Key { get; init; } = "";
        /// <summary>
        /// 具体的task内容
        /// </summary>
        public string Task { get; init; } = "";
        /// <summary>
        /// 执行成功后的下一个消息
        /// </summary>
        public Guid? NextSuccessTask { get; init; }
        /// <summary>
        /// 执行失败后的下一个消息
        /// </summary>
        public Guid? NextFailureTask { get; init; }
        /// <summary>
        /// 未找到处理端的下一条消息
        /// </summary>
        public Guid? NextNotFoundTask { get; init; }
    }
}
