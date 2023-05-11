using MiniSpore.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiniSpore.Common
{

    public class SocketClient
    {
        //全局参数
        private System.Timers.Timer myTimer = null;
        public static DateTime newDateTime = DateTime.Parse("1970-01-01 00:00:00");//记录最新的从服务器接受到的数据时间
        private static int inteval = 30000; //心跳间隔
        private static Main form;//主界面对话框
        public Socket clientSocket = null; //客户端Socket
        private string strServerIP = "";
        private int nServerPort = 0;

        public SocketClient(Main form1, string serverIP, int serverPort)
        {
            form = form1;
            strServerIP = serverIP;
            nServerPort = serverPort;
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
                    myTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
                    myTimer.Interval = inteval;
                }
            }
            catch (Exception ex)
            {

                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 定时发送心跳帧
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (clientSocket == null || newDateTime.AddMinutes(5) < DateTime.Now || !clientSocket.Connected)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "检测到与远程服务器通讯异常，设备将主动断开，并重新连接！");
                    myTimer.Enabled = false;
                    DebOutPut.DebLog("心跳帧终止！");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "心跳帧终止！");
                    CloseSocket();
                    Thread.Sleep(60000);
                    connectToSever();
                }
                else if (clientSocket != null && clientSocket.Connected)
                {
                    SendKeepLive();
                }
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }


        /// <summary>
        /// 发送连接保持帧
        /// </summary>
        public void SendKeepLive()
        {
            try
            {
                Protocol protocol = new Protocol()
                {
                    message = "keep-alive",
                    devId = Param.DeviceID,
                    func = 100,
                    err = ""
                };
                SendMsg(JsonConvert.SerializeObject(protocol));
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }

        /// <summary>
        /// 关闭Socket
        /// </summary>
        public void CloseSocket()
        {
            try
            {
                if (clientSocket != null && clientSocket.Connected)
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    Thread.Sleep(10);
                    clientSocket.Disconnect(false);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "设备主动断开连接");
                }
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
            finally
            {
                if (clientSocket != null)
                {
                    clientSocket.Close();
                    clientSocket = null;
                }
            }
        }
        /// <summary>
        /// 数据发送
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public bool SendMsg(string cmd)
        {
            try
            {
                if (clientSocket != null && clientSocket.Connected)
                {
                    JObject jo = (JObject)JsonConvert.DeserializeObject(cmd);
                    string func = jo["func"].ToString();
                    byte[] sendBytes = Encoding.GetEncoding("gb2312").GetBytes(cmd + "\r\n");
                    int n = clientSocket.Send(sendBytes);
                    if (n != sendBytes.Length)
                    {
                        if (func == "100")
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Socket事件_发送失败:" + cmd);
                        else
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Socket事件_发送失败:" + cmd);
                        DebOutPut.DebLog("Socket事件_发送失败:" + cmd);
                        return false;
                    }
                    else
                    {
                        if (func == "100")
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Socket事件_发送成功:" + cmd);
                        else
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Socket事件_发送成功:" + cmd);
                        DebOutPut.DebLog("Socket事件_发送成功:" + cmd);
                        return true;
                    }
                }
                else
                {
                    DebOutPut.DebLog("Socket事件_数据发送，检测到连接异常！");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Socket事件，检测到连接异常！");
                    CloseSocket();
                    return false;
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                CloseSocket();
                return false;
            }
        }
        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <returns></returns>
        public void connectToSever()
        {
            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(strServerIP, nServerPort);
                if (clientSocket.Connected)
                {
                    newDateTime = DateTime.Now;
                    DebOutPut.DebLog("与服务器连接成功！");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "与服务器连接成功！");
                    Receive(clientSocket);
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog("与服务器连接失败:" + ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "与服务器连接失败:" + ex.ToString());
            }
            finally
            {
                if (myTimer == null)
                {
                    InitTimer();
                    myTimer.Enabled = true;
                    DebOutPut.DebLog("心跳帧启动！");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "心跳帧启动！");
                }
                else if (myTimer.Enabled == false)
                {
                    myTimer.Enabled = true;
                    DebOutPut.DebLog("心跳帧启动！");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "心跳帧启动！");
                }

            }
        }

        /// <summary>
        /// 接收
        /// </summary>
        /// <param name="client"></param>
        private void Receive(Socket client)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    StateObject state = new StateObject();
                    state.workSocket = client;
                    client.BeginReceive(state.buffer, 0, StateObject.BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
                }
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 回调函数
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {

            StateObject state = (StateObject)ar.AsyncState;
            Socket socket = state.workSocket;
            try
            {
                if (socket != null && socket.Connected)
                {
                    int length = socket.EndReceive(ar);
                    string message = Encoding.GetEncoding("gb2312").GetString(state.buffer, 0, length);
                    if (message != "")
                    {
                        form.DealMsg(message);//处理消息
                        newDateTime = DateTime.Now;
                        socket.BeginReceive(state.buffer, 0, StateObject.BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
                    }
                }
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                CloseSocket();
            }
        }

    }


    public class StateObject
    {
        public Socket workSocket = null;
        public const int BUFFER_SIZE = 1024;
        public byte[] buffer = new byte[BUFFER_SIZE];
        public StringBuilder sb = new StringBuilder();
    }

}
