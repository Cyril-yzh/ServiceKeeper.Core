using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core
{
    public class TaskDetail
    {
        /// <summary>
        /// Task的唯一Id
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; } = null!;
        ///// <summary>
        ///// 任务成功后下一个任务的唯一Id
        ///// </summary>
        //public Guid? NextSuccessTaskId { get; init; }
        ///// <summary>
        ///// 任务失败后下一个任务的唯一Id
        ///// </summary>
        //public Guid? NextFailureTaskId { get; init; }
        ///// <summary>
        ///// 成功后下一个任务的唯一Id
        ///// </summary>
        //public Guid? NextNotFoundTaskId { get; init; }
        /// <summary>
        /// 具体的task内容
        /// </summary>
        public string Task { get; init; } = null!;
    }
}
