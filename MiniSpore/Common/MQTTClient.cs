using Aliyun.OSS;
using MiniSpore.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

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
                    Model.ProtocolModel message = new Model.ProtocolModel()
                    {
                        message = "keep-alive",
                        devId = Param.DeviceID,
                        func = 100,
                        err = ""
                    };
                    publishMessage(JsonConvert.SerializeObject(message));
                }
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }
        /// <summary>
        /// 实例化MQTT客户端
        /// </summary>
        public void BuildMqttClient()
        {
            try
            {
                if (string.IsNullOrEmpty(Param.MQTTServerIP) || string.IsNullOrEmpty(Param.MQTTServerPort))
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "未设置MQTT服务器地址或端口号信息");
                    return;
                }
                client = new MqttClient(Param.MQTTServerIP, int.Parse(Param.MQTTServerPort), false, MqttSslProtocols.TLSv1_2, null, null);
                client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;//接收消息
                client.ConnectionClosed += Client_ConnectionClosed;//服务器主动断开重新连接
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, string.Format("实例化MQTT客户端异常：{0}", ex.ToString()));
            }
        }

        /// <summary>
        /// MQTT接收消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                //处理接收到的消息  
                string strReceiveMessage = Encoding.UTF8.GetString(e.Message);
                if (form.InvokeRequired)
                {
                    MqttClient.MqttMsgPublishEventHandler setpos = new MqttClient.MqttMsgPublishEventHandler(Client_MqttMsgPublishReceived);
                    form.Invoke(setpos, new object[] { sender }, e);
                }
                else
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, string.Format("主题【{0}】，接收消息：{1}", e.Topic, strReceiveMessage));
                    if (string.IsNullOrEmpty(strReceiveMessage))
                    {
                        return;
                    }
                    Model.ProtocolModel modelMessage = JsonConvert.DeserializeObject<Model.ProtocolModel>(strReceiveMessage);
                    if (modelMessage.devId != Param.DeviceID)
                    {
                        return;
                    }
                    form.DealMsg(strReceiveMessage);
                }
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, string.Format("接收异常消息：{0}", ex.ToString()));
            }

        }

        /// <summary>
        /// 关闭MQTT通讯
        /// </summary>
        public void CloseMQTT()
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }

        /// <summary>
        /// Mqtt连接断开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_ConnectionClosed(object sender, EventArgs e)
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "mqtt连接：" + client.IsConnected);
            if (!client.IsConnected)
            {
                TryContinueConnect(); // 尝试重连
            }
        }

        /// <summary>
        ///  自动重连主体
        /// </summary>
        private void TryContinueConnect()
        {
            if (client.IsConnected)
                return;
            Thread retryThread = new Thread(new ThreadStart(delegate
            {
                while (client == null || !client.IsConnected)
                {
                    if (client.IsConnected)
                        break;
                    if (client == null)
                    {
                        BuildMqttClient();
                        MqttConnect();
                        Thread.Sleep(3000);
                        continue;
                    }
                    try
                    {
                        MqttConnect();
                    }
                    catch (Exception ex)
                    {
                        DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "重新连接异常:" + ex.ToString());
                    }
                    // 如果还没连接不符合结束条件则睡2秒
                    if (!client.IsConnected)
                        Thread.Sleep(2000);
                }
            }));
            retryThread.Start();
        }


        /// <summary>
        /// 发起一次连接，连接成功则订阅相关主题 
        /// </summary>
        public void MqttConnect()
        {
            try
            {
                if (client == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(Param.MQTTAccount) || string.IsNullOrEmpty(Param.MQTTPassword))
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "未设置MQTT服务器ClientID、账号、密码信息");
                    return;
                }
                string strClientID = Param.MQTTClientID;
                if (string.IsNullOrEmpty(Param.MQTTClientID))
                {
                    strClientID = Guid.NewGuid().ToString();
                }

                client.Connect(strClientID,
                    Param.MQTTAccount,
                    Param.MQTTPassword,
                    true, // 清理会话，默认为true。设置为false可以接收到QoS 1和QoS 2级别的离线消息
                    50 // 客户端向【服务端】发送心跳的时间间隔，默认50秒，设置成0代表不启用心跳，注意不是向服务器的两个连接方
                    );

                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "mqtt连接：" + client.IsConnected);
                if (client.IsConnected)
                {
                    client.Subscribe(new string[] { strSubscribeTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, string.Format("订阅主题{0}成功", strSubscribeTopic));
                }
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "MqttConnect连接异常:" + ex.ToString());
            }
            finally
            {
                //开启定时器，定时发送心跳帧
                if (myTimer == null)
                {
                    InitTimer();
                    myTimer.Enabled = true;
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "心跳帧启动！");
                }
                else if (myTimer.Enabled == false)
                {
                    myTimer.Enabled = true;
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "心跳帧启动！");
                }
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
