using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSpore.Model
{
    public class BluetoothModel
    {
        public int Func { get; set; }
        public object Message { get; set; }
    }

    /// <summary>
    /// 设备参数
    /// </summary>
    public class DeviceParams
    {
        public string DeviceID { get; set; }
        public int CommunicateMode { get; set; }
        public string ServerIP { get; set; }
        public int ServerPort { get; set; }
        public string Action { get; set; }
        public int WorkMode { get; set; }
        public int CollectTime { get; set; }
        public int TimeSlot1 { get; set; }
        public int TimeSlot2 { get; set; }
        public int TimeSlot3 { get; set; }
        public int ChooseImageCount { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public int IsNetwork { get; set; }
        public string ErrorCode { get; set; }

    }

    public class MotorShaft
    {
        /// <summary>
        /// 0顺时针 1逆时针
        /// </summary>
        public int Way { get; set; }
        public int Step { get; set; }
    }

}
