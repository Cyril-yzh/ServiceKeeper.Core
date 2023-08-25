using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core
{
    public enum ServiceStatus
    {
        /// <summary>
        /// 主状态
        /// </summary>
        Active,
        /// <summary>
        /// 备状态
        /// </summary>
        Standby,
    }
}
