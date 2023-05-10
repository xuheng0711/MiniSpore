using MiniSpore.Model;
using Newtonsoft.Json;
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

        public MQTTClient(Main form1, MQTTModel param)
        {
            form = (Main)form1;
            //MQTT对象
            mQTTModel = param;
            //订阅主题
            strSubscribeTopic = string.Format("downrive/minispore/json/{0}", Param.DeviceID);
            //发布主题
            strPublishTopic = string.Format("upstream/minispore/json/{0}", Param.DeviceID);
        }

        /// <summary>
        /// 心跳计时器初始化
        /// </summary>
        public void InitTimer()
        {
            try
            {
                if (myTimer == null)
                {
                    myTimer = new System.Timers.Timer(inteval);
                    myTimer.Elapsed += new System.Timers.ElapsedEventHandler(SendKeepLive);
                    myTimer.Interval = inteval;
                }
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }


        /// <summary>
        /// 定时发送心跳帧
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void SendKeepLive(object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (client != null && client.IsConnected)
                {
                    MQTTMessage message = new MQTTMessage()
                    {
                        message = "keep-alive",
                        devId = Param.DeviceID,
                        func = 100,
                        err = ""
                    };
                    string jsonData=JsonConvert.SerializeObject(message);
                    publishMessage(jsonData,true);
                }
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }


        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="message"></param>
        public void publishMessage(string message, bool isHeartbeat = false)
        {
            lock (locker)
            {
                if (client.IsConnected)
                {
                    byte[] bMessage = Encoding.UTF8.GetBytes(message);
                    client.Publish(strPublishTopic, bMessage, 2, false);
                    DebOutPut.WriteLog(LogType.Normal, isHeartbeat == true ? LogDetailedType.KeepAliveLog : LogDetailedType.Ordinary, string.Format("主题【{0}】，发布消息：{1}", strPublishTopic, message));
                }
            }
        }

    }
}
