using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ServiceKeeper.Core
{
    /// <summary>
    /// MQ事件处理回复
    /// </summary>
    internal class MQResponse
    {
        public MQResponse(string? taskName, Guid? guid, EventResult eventResult)
        {
            TaskName = taskName ?? "未找到任务名";
            Guid = guid;
            EventResult = eventResult;
        }
        public string TaskName { get; set; }
        /// <summary>
        /// 任务的Id
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Guid? Guid { get; set; }
        public EventResult EventResult { get; set; }

    }
    /// <summary>
    /// MQ事件处理回复
    /// </summary>
    public class EventResult
    {
        public EventResult(Code code, string? message)
        {
            Code = code;
            Message = message;
        }

        public Code Code { get; set; }
        /// <summary>
        /// 可选:回复消息
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; }
    }
    public enum Code
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success,
        /// <summary>
        /// 失败
        /// </summary>
        Failure,
        /// <summary>
        /// 解析错误
        /// </summary>
        ParseError,
    }
}
