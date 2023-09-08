using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ServiceKeeper.Core
{
    public class MqReply
    {
        public ErrorCode ErrorCode { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorMessage { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Guid? NextTask { get; set; }
    }
    public enum ErrorCode
    {
        Success = 10000,
        Failure = 10001,
        ParseError = 10002,
        NotFound = 10003,
    }
}
