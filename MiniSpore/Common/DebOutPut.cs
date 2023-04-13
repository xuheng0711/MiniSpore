﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSpore.Common
{
    public class DebOutPut
    {
        private const int saveLogDay = 15;
#if DEBUG
        /// <summary>
        /// DebView模式，0为发布版  1位测试版  2位专业版
        /// </summary>
        public static int isDebView = 1;
#else
        /// <summary>
        /// DebView模式，0为发布版  1位测试版  2位专业版
        /// </summary>
        public static int isDebView = 0;
#endif

        /// <summary>
        /// 输出信息
        /// </summary>
        /// <param name="message">信息</param>
        public static void DebLog(string message)
        {
            if (isDebView == 1)
                Debugger.Log(0, null, "Debug:" + message + "\n");
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="sInfo">日志信息</param>
        /// <param name="isDebug">是否开启debug,如果为true表示不是异常日志</param>
        public static void WriteLog(LogType logType, LogDetailedType logDetailedType, string sInfo, bool isDebug = false)
        {
            try
            {
                if (!isDebug)
                {
                    string sDir = "";
                    switch (logType)
                    {
                        case LogType.Normal:
                            sDir = SavePath(LogType.Normal, logDetailedType);
                            break;
                        case LogType.Error:
                            sDir = SavePath(LogType.Error, logDetailedType);
                            break;
                        default:
                            break;
                    }
                    if (sDir != "")
                    {
                        //文件流
                        FileStream fileStream = new FileStream(sDir, FileMode.Append, FileAccess.Write);
                        //获取当前时间
                        DateTime dateTime = DateTime.Now;
                        string sTime = dateTime.ToString(Param.DateFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo) + ":" + dateTime.Millisecond;
                        //将当前时间以及日志信息写入文件
                        string data = "\r\n" + "[" + sTime + "]" + " 信息:" + sInfo + "\r\n";
                        //拿到长度 
                        int tlen = Encoding.ASCII.GetCharCount(Encoding.UTF8.GetBytes(data.ToCharArray(), 0, data.Length));
                        byte[] writeByte = Encoding.UTF8.GetBytes(data.ToCharArray(), 0, data.Length);
                        fileStream.Write(writeByte, 0, tlen);
                        //刷新流
                        fileStream.Flush();
                        //关闭流
                        fileStream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                DebLog(ex.ToString());
            }
        }

        /// <summary>
        /// 保存路径
        /// </summary>
        private static string SavePath(LogType logType, LogDetailedType logDetailedType)
        {
            try
            {
                //日志文件名称
                string logName = DateTime.Now.ToString("yyyyMMdd");
                switch (logDetailedType)
                {
                    case LogDetailedType.KeepAliveLog:
                        logName += "_KeepAlive.log";
                        break;
                    case LogDetailedType.ComLog:
                        logName += "_Com.log";
                        break;
                    case LogDetailedType.Ordinary:
                        logName += "_Ordinary.log";
                        break;
                    default:
                        break;
                }
                //一级基目录
                string sDir = System.Windows.Forms.Application.StartupPath + "\\" + "Log";
                if (!Directory.Exists(sDir))
                    Directory.CreateDirectory(sDir);
                //二级基目录
                switch (logType)
                {
                    case LogType.Normal:
                        sDir += "\\NormalLog";
                        if (!Directory.Exists(sDir))
                            Directory.CreateDirectory(sDir);
                        Tools.DeleteOldFiles(sDir, saveLogDay);
                        sDir += "\\" + logName;
                        break;
                    case LogType.Error:
                        sDir += "\\ErrorLog";
                        if (!Directory.Exists(sDir))
                            Directory.CreateDirectory(sDir);
                        Tools.DeleteOldFiles(sDir, saveLogDay);
                        sDir += "\\" + logName;
                        break;
                    default:
                        break;
                }
                return sDir;
            }
            catch (Exception ex)
            {
                DebLog(ex.ToString());
                return "";
            }

        }
    }
}
