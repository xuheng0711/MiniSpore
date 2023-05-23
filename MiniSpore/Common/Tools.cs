using Aliyun.OSS;
using Microsoft.Win32;
using MiniSpore.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MiniSpore.Common
{
    public class Tools
    {

        /// <summary>
        /// byte[]转hexString
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ByteToHexStr(byte[] bytes)
        {
            try
            {
                string returnStr = "";
                if (bytes != null)
                {
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        returnStr += bytes[i].ToString("X2");
                    }
                }
                return returnStr;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return "";
            }
        }

        /// <summary>
        /// 图片读取
        /// </summary>
        /// <param name="fileName">路径</param>
        /// <returns></returns>
        public static Bitmap FileToBitmap(string fileName)
        {
            // 打开文件    
            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            // 读取文件的 byte[]    
            byte[] bytes = new byte[fileStream.Length];
            fileStream.Read(bytes, 0, bytes.Length);
            fileStream.Close();
            // 把 byte[] 转换成 Stream    
            Stream stream = new MemoryStream(bytes);

            stream.Read(bytes, 0, bytes.Length);
            // 设置当前流的位置为流的开始    
            stream.Seek(0, SeekOrigin.Begin);

            MemoryStream mstream = null;
            try
            {
                mstream = new MemoryStream(bytes);
                Bitmap bmp = new Bitmap(stream);
                //return new Bitmap((Image)new Bitmap(stream));
                return bmp;
            }
            catch (ArgumentNullException ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return null;
            }
            catch (ArgumentException ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return null;
            }
            finally
            {
                stream.Close();
            }
        }

        /// <summary>
        /// 保存图像
        /// </summary>
        /// <param name="img"></param>
        /// <param name="imgName"></param>
        /// <returns></returns>
        public static bool SaveImage(Image img, string imgName)
        {
            try
            {
                string path = Param.basePath + "\\GrabImg\\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                if (Directory.Exists(path))
                {
                    path += imgName;
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    img.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "保存图片失败！" + ex.ToString());
                return false;
            }

        }

        /// <summary>  
        /// 修改程序在注册表中的键值  
        /// </summary>  
        /// <param name="isAuto">true:开机启动,false:不开机自启</param> 
        public static void AutoStart(bool isAuto)
        {
            try
            {
                string ShortFileName = Application.ProductName;
                if (isAuto == true)
                {
                    //获取本地计算机的注册表
                    RegistryKey R_local = Registry.CurrentUser;
                    //注册表里面创建一个新子项或打开一个现有子项进行访问
                    RegistryKey R_run = R_local.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run\");
                    string[] keys = R_run.GetValueNames();

                    bool bIsCreate = true;
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (keys[i] == ShortFileName)
                        {

                            string val = R_run.GetValue(ShortFileName) + "";
                            if (!string.Equals(val, Application.ExecutablePath))
                            {
                                R_run.DeleteValue(ShortFileName, false);//删除指定值
                            }
                            else
                            {
                                bIsCreate = false;
                            }
                            break;
                        }
                    }
                    if (bIsCreate)
                    {
                        //要执行文件的名称及路径
                        R_run.SetValue(ShortFileName, Application.ExecutablePath);
                        //关闭
                        R_run.Close();
                        R_local.Close();
                    }
                }
                else
                {
                    RegistryKey R_local = Registry.LocalMachine;
                    RegistryKey R_run = R_local.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run\");
                    R_run.DeleteValue(ShortFileName, false);//删除指定值
                    R_run.Close();
                    R_local.Close();
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }

        /// <summary>
        /// 计算机重启
        /// </summary>
        public static void WinRestart()
        {
            DebOutPut.DebLog("计算机自动重启！");
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "计算机自动重启");
            System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
            myProcess.StartInfo.FileName = "cmd.exe";//启动cmd命令
            myProcess.StartInfo.UseShellExecute = false;//是否使用系统外壳程序启动进程
            myProcess.StartInfo.RedirectStandardInput = true;//是否从流中读取
            myProcess.StartInfo.RedirectStandardOutput = true;//是否写入流
            myProcess.StartInfo.RedirectStandardError = true;//是否将错误信息写入流
            myProcess.StartInfo.CreateNoWindow = true;//是否在新窗口中启动进程
            myProcess.Start();//启动进程
            myProcess.StandardInput.WriteLine("shutdown -r -t 0");//执行重启计算机命令
        }
        /// <summary>
        /// 求指定时间和当前时间相差的秒数
        /// </summary>
        /// <param name="time">指定时间</param>
        /// <param name="currTime">当前时间</param>
        /// <returns></returns>
        public static string GetNowTimeSpanSec(DateTime time, DateTime currTime)
        {
            //指定时间减去当前时间
            TimeSpan ts = time.Subtract(currTime);
            int sec = (int)ts.TotalSeconds;
            return sec.ToString();
        }
        /// <summary>
        /// 删除文件夹
        /// </summary>
        /// <param name="dir"></param>
        public static void DeleteFolder(string dir)
        {
            try
            {
                foreach (string d in Directory.GetFileSystemEntries(dir))
                {
                    if (File.Exists(d))
                    {
                        FileInfo fi = new FileInfo(d);
                        if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)
                            fi.Attributes = FileAttributes.Normal;
                        File.Delete(d);//直接删除其中的文件   
                    }
                    else
                        DeleteFolder(d);//递归删除子文件夹   
                }
                Directory.Delete(dir);//删除已空文件夹   
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 判断当前时间是否在工作时间段内
        /// </summary>
        /// <param name="timeStr">当前时间</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns></returns>
        public static bool GetTimeSpan(string timeStr, string startTime, string endTime)
        {
            try
            {
                string _strWorkingDayAM = startTime;//工作时间上午08:30
                string _strWorkingDayPM = endTime;
                TimeSpan dspWorkingDayAM = DateTime.Parse(_strWorkingDayAM).TimeOfDay;
                TimeSpan dspWorkingDayPM = DateTime.Parse(_strWorkingDayPM).TimeOfDay;
                //string time1 = "2017-2-17 8:10:00";
                DateTime t1 = Convert.ToDateTime(timeStr);
                TimeSpan dspNow = t1.TimeOfDay;
                if (dspNow > dspWorkingDayAM && dspNow < dspWorkingDayPM)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 串口检测
        /// </summary>
        public static List<string> SerialPortTesting()
        {
            try
            {
                //HKEY_LOCAL_MACHINE\HARDWARE\DEVICEMAP\SERIALCOMM
                RegistryKey hklm = Registry.LocalMachine;
                RegistryKey software11 = hklm.OpenSubKey("HARDWARE");
                RegistryKey software = software11.OpenSubKey("DEVICEMAP");
                RegistryKey sitekey = software.OpenSubKey("SERIALCOMM");
                if (sitekey == null)
                {
                    return null;
                }
                //获取当前子键下所有项名字
                string[] termName = sitekey.GetValueNames();
                List<string> termValue = new List<string>();
                //获得当前子键下项的值
                for (int i = 0; termName != null && i < termName.Length; i++)
                {
                    termValue.Add((string)sitekey.GetValue(termName[i]));
                }
                if (termValue.Count > 0)
                {
                    return termValue;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// 软件重新启动
        /// </summary>
        public static void RestStart()
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "程序被重启！");
            PubField.mutex.Close();
            string path = System.Windows.Forms.Application.ExecutablePath;
            Win32API.ShellExecute(IntPtr.Zero, "open", path, "/S", "", ShellExecute_ShowCommands.SW_SHOWNORMAL);
            System.Environment.Exit(0);
        }

        /// <summary>
        /// 获取当前登录用户(可用于管理员身份运行)
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentUser()
        {
            IntPtr buffer;
            uint strLen;
            int cur_session = -1;
            var username = "SYSTEM";
            if (Win32API.WTSQuerySessionInformation(IntPtr.Zero, cur_session, WTSInfoClass.WTSUserName, out buffer, out strLen) && strLen > 1)
            {
                username = Marshal.PtrToStringAnsi(buffer);
            }
            return username;
        }

        public static double GetCPUTemperature()
        {
            double cpuTemperature = 0;
            ManagementObjectSearcher mos = new ManagementObjectSearcher(@"root\WMI", "Select * From MSAcpi_ThermalZoneTemperature");
            foreach (System.Management.ManagementObject mo in mos.Get())
            {
                cpuTemperature = Convert.ToDouble(Convert.ToDouble(mo.GetPropertyValue("CurrentTemperature").ToString()) - 2732) / 10;
            }
            return cpuTemperature;
        }

        /// <summary>
        /// 删除文件夹strDir中nDays天以前的文件
        /// </summary>
        /// <param name="dir">目录路径</param>
        /// <param name="days">天数</param>
        public static void DeleteOldFiles(string dir, int days)
        {
            try
            {
                if (!Directory.Exists(dir) || days < 1)
                    return;
                var now = DateTime.Now;
                foreach (var f in Directory.GetFileSystemEntries(dir).Where(f => File.Exists(f)))
                {
                    var t = File.GetCreationTime(f);
                    var elapsedTicks = now.Ticks - t.Ticks;
                    var elapsedSpan = new TimeSpan(elapsedTicks);
                    if (elapsedSpan.TotalDays > days)
                        File.Delete(f);
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 上传图片至阿里云OSS服务器
        /// </summary>
        /// <param name="picPath"></param>
        /// <returns></returns>
        public static string UploadImageAliOSS(string picPath)
        {
            string picAliOssUrl = "";
            string fileName = System.IO.Path.GetFileName(picPath);
            //填写Object完整路径，完整路径中不能包含Bucket名称
            var objectName = string.Format("minispore/{0}", fileName);
            // 创建OssClient实例。

            if (!string.IsNullOrEmpty(Param.OssEndPoint) && !string.IsNullOrEmpty(Param.OssAccessKeyId) && !string.IsNullOrEmpty(Param.OssAccessKeySecret) && !string.IsNullOrEmpty(Param.OssBucketName) && !string.IsNullOrEmpty(Param.OSS_Url))
            {
                var client = new OssClient(Param.OssEndPoint, Param.OssAccessKeyId, Param.OssAccessKeySecret);
                client.PutObject(Param.OssBucketName, objectName, picPath);

                picAliOssUrl = Param.OSS_Url + objectName;
            }
            else
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "阿里云OSS配置信息不完整");
            }
            return picAliOssUrl;
        }


    }
}
