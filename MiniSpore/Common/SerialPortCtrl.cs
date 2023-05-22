using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiniSpore.Common
{
    public class SerialPortCtrl
    {
        //创建锁对象
        private static readonly Object locker = new object();

        /// <summary>
        /// 发送字节指令（只发送一次）
        /// </summary>
        /// <param name="serialPort"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public byte[] SendCmd(SerialPort serialPort, byte[] cmd)
        {
            try
            {
                if (serialPort == null || !serialPort.IsOpen)
                {
                    DebOutPut.DebLog("串口未打开!");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "串口未打开!");
                    return null;
                }
                serialPort.DiscardInBuffer();
                string sendCmd = Tools.ByteToHexStr(cmd);
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送命令:" + sendCmd);
                serialPort.Write(cmd, 0, cmd.Length);
                Thread.Sleep(300);
                int ilen = serialPort.BytesToRead;
                if (ilen <= 0)
                {
                    return null;
                }
                byte[] readBytes = new byte[ilen];
                serialPort.Read(readBytes, 0, ilen);
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "接收终端回应:" + Tools.ByteToHexStr(readBytes));
                return readBytes;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return null;
            }
        }


        /// <summary>
        /// 数据发送 
        /// </summary>
        /// <param name="serialPort">serialPort</param>
        /// <param name="Msg">数据</param>
        public string SendMsg(SerialPort serialPort, string Msg)
        {
            try
            {
                if (serialPort == null || !serialPort.IsOpen)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "串口未打开!");
                    return "";
                }
                serialPort.DiscardInBuffer();
                Msg += "\r\n";
                DebOutPut.DebLog("发:" + Msg);
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送信息:" + Msg);
                string recStr = "";
                int exectCount = 0;
                while (exectCount < 3)
                {
                    serialPort.Write(Msg);
                    recStr = serialPort.ReadLine();
                    if (string.IsNullOrEmpty(recStr))
                    {
                        exectCount++;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                //串口发送指令
                if (string.IsNullOrEmpty(recStr) && exectCount == 3)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送未收到回复，指令为：" + Msg);
                }
                else
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "接到指令为：" + recStr);
                }
                return recStr;
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return "";
            }

        }

        /// <summary>
        /// 发送字节指令
        /// </summary>
        /// <param name="serialPort"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public byte[] SendCommand(SerialPort serialPort, byte[] cmd)
        {
            lock (locker)
            {
                try
                {
                    if (serialPort == null || !serialPort.IsOpen)
                    {
                        DebOutPut.DebLog("串口未打开!");
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "串口未打开!");
                        return null;
                    }
                    serialPort.DiscardInBuffer();
                    string sendCmd = Tools.ByteToHexStr(cmd);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送命令:" + sendCmd);

                    int index = 0;
                    for (int i = 0; i < 3; i++)
                    {
                        serialPort.Write(cmd, 0, cmd.Length);
                        Thread.Sleep(300);
                        if (serialPort.BytesToRead <= 0)
                            index++;
                        else
                            break;
                    }
                    if (index == 3)
                    {
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "指令连续3次发送未收到回复，指令为：" + sendCmd.ToUpper());
                        return null;
                    }
                    int ilen = serialPort.BytesToRead;
                    if (ilen <= 0)
                    {
                        return null;
                    }
                    byte[] readBytes = new byte[ilen];
                    serialPort.Read(readBytes, 0, ilen);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "接收终端回应:" + Tools.ByteToHexStr(readBytes));
                    return readBytes;
                }
                catch (Exception ex)
                {
                    DebOutPut.DebLog(ex.ToString());
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                    return null;
                }
            }
        }


        /// <summary>
        /// 关闭串口
        /// </summary>
        /// <param name="serialPort"></param>
        /// <returns></returns>
        public bool CloseSerialPort(SerialPort serialPort)
        {
            try
            {
                serialPort.Close();
                return true;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return false;
            }
        }

    }
}
