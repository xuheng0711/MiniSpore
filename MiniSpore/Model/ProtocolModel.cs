using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSpore.Model
{
    public class ProtocolModel
    {
        /// <summary>
        /// 功能码
        /// </summary>
        public int func { get; set; }
        /// <summary>
        /// 错误信息
        /// </summary>
        public string err { get; set; }
        /// <summary>
        /// 设备号
        /// </summary>
        public string devId { get; set; }
        /// <summary>
        /// 信息
        /// </summary>
        public object message { get; set; }
    }

    public class CollectInfo
    {
        public string collectTime { get; set; }
        public string picUrl { get; set; }
    }

}
