using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ServiceKeeper.Core.Entity
{
    /// <summary>
    /// 工作任务实体
    /// </summary>
    public class TaskEntity
    {
        /// <summary>
        /// Task的唯一Id
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// 发送至处理服务的Key
        /// </summary>
        public string PublishKey { get; private set; } = null!;
        public bool IsFirstNode { get; private set; }
        /// <summary>
        /// 将任务详细信息存储为 JSON 字符串
        /// </summary>
        public string TaskJson { get; private set; } = null!;
        /// <summary>
        /// 将任务触发器存储为 JSON 字符串
        /// </summary>
        public string? TriggerJson { get; private set; }
        [NotMapped]
        private TaskDetail? detail;
        [NotMapped] // 该属性不会映射到数据库
        public TaskDetail Detail
        {
            get
            {
                if (detail != null) return detail;
                TaskDetail result = JsonSerializer.Deserialize<TaskDetail>(TaskJson) ?? throw new Exception("无法将ValueJson反序列化为TaskDetail");
                return detail = result;
            }
            //set => this.detail = value;
        }
        [NotMapped]
        private TaskTrigger? trigger;

        [NotMapped] // 该属性不会映射到数据库
        public TaskTrigger? Trigger
        {
            get
            {
                try
                {
                    if (trigger != null) return trigger;
                    if (!IsFirstNode) return null;
                    trigger = JsonSerializer.Deserialize<TaskTrigger>(TriggerJson!) ?? throw new Exception("无法将TriggerJson反序列化为TaskTrigger");
                    return trigger;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }

            }
            //set => this.trigger = value;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            if (obj is not TaskEntity other) return false;

            return Id == other.Id
                && IsFirstNode == other.IsFirstNode
                && TaskJson == other.TaskJson
                && TriggerJson == other.TriggerJson;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, IsFirstNode, TaskJson, TriggerJson);
        }
    }
}
