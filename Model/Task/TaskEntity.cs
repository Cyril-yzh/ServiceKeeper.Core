using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ServiceKeeper.Core
{
    /// <summary>
    /// 工作任务实体
    /// </summary>
    public class TaskEntity
    {
        /// <summary>
        /// 发送至处理服务的Key
        /// </summary>
        public string PublishKey { get; set; } = null!;
        public TaskDetail Detail { get; set; } = null!;
        public TaskTrigger? Trigger { get; set; }
        public TaskEntity? NextSuccessTask { get; set; }
        public TaskEntity? NextFailureTask { get; set; }
        public TaskEntity? NextNotFoundTask { get; set; }
    }
}
