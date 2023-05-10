using MiniSpore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;

namespace MiniSpore.Common
{
    public class MQTTClient
    {
        private static Main form;//主界面对话框
        MQTTModel mQTTModel = null;//MQTT对象
        /// <summary>
        /// 订阅主题
        /// </summary>
        private string strSubscribeTopic = "";

        /// <summary>
        /// 发布主题
        /// </summary>
        private string strPublishTopic = "";
        /// <summary>
        /// MQTT
        /// </summary>
        public MqttClient client;
        /// <summary>
        /// 程序锁
        /// </summary>
        private readonly object locker = new object();

        /// <summary>
        /// 定时发送心跳
        /// </summary>
        private System.Timers.Timer myTimer = null;
        private int inteval = 30000; //心跳间隔


    }
}
