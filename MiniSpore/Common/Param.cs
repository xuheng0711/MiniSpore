using MiniSpore.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSpore.Common
{
    public class Param
    {
        public static string basePath = System.Windows.Forms.Application.StartupPath;
        //设备编号
        public static string DeviceID = "";
        public static string SerialPort = "";
        public static string Baudrate = "";
        public static string CollectTime = "";
        public static string DateFormat = "yyyy-MM-dd HH:mm:ss"; 
        //通讯方式
        public static string CommunicateMode = "";//0:MQTT 1:Socket
        public static string MQTTServerIP = "";
        public static string MQTTServerPort = "";
        public static string SocketServerIP = "";
        public static string SocketServerPort = "";

        //定时运行采集时间
        public static string CollectHour = "";
        public static string CollectMinute = "";
        public static string isWinRestart = "";//每天电脑是否自动重启  0否  1是
        
        #region MQTT服务器
        public static string MQTTClientID = "";
        public static string MQTTAccount = "";
        public static string MQTTPassword = "";
        #endregion

        #region 阿里云OSS存储
        public static string OssEndPoint = "";
        public static string OssAccessKeyId = "";
        public static string OssAccessKeySecret = "";
        public static string OssBucketName = "";
        public static string OSS_Url = "";
        #endregion

        /// <summary>
        /// 初始化读取程序参数
        /// </summary>
        /// <param name="configfileName">配置文件名称Config.ini</param>
        public static void Init_Param(string configfileName)
        {
            try
            {
                DeviceID = Read_ConfigParam(configfileName, "Config", "DeviceID");//设备编号
                SerialPort= Read_ConfigParam(configfileName, "Config", "SerialPort");
                Baudrate = Read_ConfigParam(configfileName, "Config", "Baudrate");
                CollectTime = Read_ConfigParam(configfileName, "Config", "CollectTime");//采集时长
                CommunicateMode = Read_ConfigParam(configfileName, "Config", "CommunicateMode");
                //MQTT协议
                MQTTServerIP = Read_ConfigParam(configfileName, "Config", "MQTTServerIP");
                MQTTServerPort = Read_ConfigParam(configfileName, "Config", "MQTTServerPort");
                //Socket协议
                SocketServerIP = Read_ConfigParam(configfileName, "Config", "SocketServerIP");
                SocketServerPort = Read_ConfigParam(configfileName, "Config", "SocketServerPort");
                //定时时间
                CollectHour = Read_ConfigParam(configfileName, "Config", "CollectHour");
                CollectMinute = Read_ConfigParam(configfileName, "Config", "CollectMinute");
                isWinRestart = Read_ConfigParam(configfileName, "Config", "isWinRestart");

                HttpRequest httpRequest = new HttpRequest();
                string url = string.Format("http://nyzbwlw.com/situation/http/mqtt/getClientMqtt?eqCode={0}", DeviceID);
                string strResponse = httpRequest.Get(url);
                if (!string.IsNullOrEmpty(strResponse))
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, string.Format("http接口获取MQTT账号信息：{0}", strResponse));
                    MQTTClientInfo mqttClient = JsonConvert.DeserializeObject<MQTTClientInfo>(strResponse);
                    MQTTClientID = mqttClient.result.clientId;
                    MQTTAccount = mqttClient.result.userName;
                    MQTTPassword = mqttClient.result.passwords;
                }

                OssEndPoint = Read_ConfigParam(configfileName, "AliyunOSS", "EndPoint");
                OssAccessKeyId = Read_ConfigParam(configfileName, "AliyunOSS", "AccessKeyId");
                OssAccessKeySecret = Read_ConfigParam(configfileName, "AliyunOSS", "AccessKeySecret");
                OssBucketName = Read_ConfigParam(configfileName, "AliyunOSS", "BucketName");
                OSS_Url = Read_ConfigParam(configfileName, "AliyunOSS", "OSS_Url");
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 读取配置文件参数
        /// </summary>
        /// <param name="configfileName">配置文件名称</param>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string Read_ConfigParam(string configfileName, string key, string name)
        {
            try
            {
                string configPath = basePath + "\\" + configfileName;
                StringBuilder stringBuilder = new StringBuilder(255);
                Win32API.GetPrivateProfileString(key, name, "", stringBuilder, 255, configPath);
                return stringBuilder.ToString();
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return "";
            }
        }

        /// <summary>
        /// 设置配置文件参数
        /// </summary>
        /// <param name="configfileName"></param>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void Set_ConfigParm(string configfileName, string key, string name, string value)
        {
            try
            {
                string configPath = basePath + "\\" + configfileName;
                Win32API.WritePrivateProfileString(key, name, value, configPath);
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }

    }
}
