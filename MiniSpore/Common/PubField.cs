using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiniSpore.Common
{
    public class PubField
    {
        public static Mutex mutex = null;
        /// <summary>
        /// 可执行文件目录，不包含可执行文件名称
        /// </summary>
        public static string pathBase = System.Windows.Forms.Application.StartupPath;
    }

    /// <summary>
    /// 日志类型
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// 正常日志
        /// </summary>
        Normal,
        /// <summary>
        /// 错误日志
        /// </summary>
        Error,
    }

    /// <summary>
    /// 日志详细类型
    /// </summary>
    public enum LogDetailedType
    {
        /// <summary>
        /// Socket日志
        /// </summary>
        KeepAliveLog,
        /// <summary>
        /// 串口日志
        /// </summary>
        ComLog,
        /// <summary>
        /// 普通日志
        /// </summary>
        Ordinary,
    }
}
