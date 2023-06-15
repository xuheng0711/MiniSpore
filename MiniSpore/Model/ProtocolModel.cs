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
    public class Location
    {
        public double lat;
        public double lon;
    }
    public class CollectInfo
    {
        public string collectTime { get; set; }
        public string picUrl { get; set; }
    }



    public class SettingInfo
    {
        public string WorkMode { get; set; }
        public string CollectTime { get; set; }
        public string ChooseImageCount { get; set; }
        public string TimeSlot1 { get; set; }
        public string TimeSlot2 { get; set; }
        public string TimeSlot3 { get; set; }
    }
    public class DeviceAbnormal
    {
        /// <summary>
        /// 是否震动报警
        /// </summary>
        public bool isAlarm { get; set; }
        /// <summary>
        /// 是否载玻带异常
        /// </summary>
        public bool isBand { get; set; }
    }

}
