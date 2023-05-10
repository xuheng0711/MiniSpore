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
        public static string DateFormat = "yyyy-MM-dd HH:mm:ss"; 
        //通讯方式
        public static string CommunicateMode = "";//0:MQTT 1:Socket
        public static string ServerIP = "";
        public static string ServerPort = "";

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
