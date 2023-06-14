using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using MiniSpore.Common;
using MiniSpore.Model;
using MvCamCtrl.NET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace MiniSpore
{
    public delegate void PushSettingMessage();
    public partial class Main : Form
    {
        public static PushSettingMessage pushSettingMessage;
        private string errorMessage = "";
        //拍照步数
        private int photoStep = 20;
        //位置标记
        private int step = -1;
        ////配置文件地址
        internal static string configfileName = "Config.ini";
        ResourceManager resources = new ResourceManager("MiniSpore.Properties.Resources", typeof(Main).Assembly);
        private Image imageProcess = null;
        /// <summary>
        /// 程序锁
        /// </summary>
        private static readonly Object locker = new object();
        /// <summary>
        /// 指令程序锁
        /// </summary>
        private static readonly Object cmdLocker = new object();
        //Socket服务器
        public SocketClient socketClient = null;
        //MQTT服务器
        public MQTTClient mqttClient = null;
        //串口通讯
        SerialPortCtrl serialPortCtrl = new SerialPortCtrl();
        private static Object BufForDriverLock = new Object();
        #region 相机对象
        private MyCamera m_pMyCamera = null;//相机对象
        ComboBox cbDeviceList = new ComboBox();
        MyCamera.MV_CC_DEVICE_INFO_LIST m_pDeviceList;
        bool m_bGrabbing = false;//是否采集
        UInt32 m_nBufSizeForDriver = 3072 * 2048 * 3;
        byte[] m_pBufForDriver = new byte[3072 * 2048 * 3];
        UInt32 m_nBufSizeForSaveImage = 3072 * 2048 * 3 * 3 + 2048;
        byte[] m_pBufForSaveImage = new byte[3072 * 2048 * 3 * 3 + 2048];
        #endregion

        bool isReceiveBluetooth = false;//是否接收蓝牙数据
        bool isBand = false;//载玻带是否正常
        bool isAlarm = false;//是否震动报警
        bool isX1 = false;
        bool isX2 = false;

        //传输图像是否完成
        private bool isTransferImage = false;
        /// <summary>
        /// 流程执行时间
        /// </summary>
        private DateTime executeTime;

        #region Timer控件

        /// <summary>
        /// 主流程
        /// </summary>
        int inTimer1 = 0;
        System.Timers.Timer timer1 = new System.Timers.Timer();

        /// <summary>
        /// 流程执行时间
        /// </summary>
        int inTimer2 = 0;
        System.Timers.Timer timer2 = new System.Timers.Timer();

        /// <summary>
        /// 定时执行任务
        /// </summary>
        int inTimer3 = 0;
        System.Timers.Timer timer3 = new System.Timers.Timer();

        /// <summary>
        /// 定时执行程序
        /// </summary>
        int inTimer4 = 0;
        System.Timers.Timer timer4 = new System.Timers.Timer();

        /// <summary>
        /// 设备状态
        /// </summary>
        int inTimer5 = 0;
        System.Timers.Timer timer5 = new System.Timers.Timer();

        #region 串口
        /// <summary>
        /// 主串口
        /// </summary>
        private SerialPort mainSerialPort = new SerialPort();
        /// <summary>
        /// 蓝牙串口
        /// </summary>
        private SerialPort bluetoothSerialPort = new SerialPort();
        /// <summary>
        /// GPS串口
        /// </summary>
        private SerialPort gpsSerialPort = new SerialPort();
        #endregion

        private void Timer1Start()
        {
            timer1.Start();
        }
        private void Timer1Stop()
        {
            timer1.Stop();
            Interlocked.Exchange(ref inTimer1, 0);
        }
        private void Timer2Start()
        {
            timer2.Start();
        }
        private void Timer2Stop()
        {
            timer2.Stop();
            Interlocked.Exchange(ref inTimer2, 0);
        }

        private void Timer3Start()
        {
            timer3.Start();
        }
        private void Timer3Stop()
        {
            timer3.Stop();
            Interlocked.Exchange(ref inTimer3, 0);
        }

        private void Timer4Start()
        {
            timer4.Start();
        }
        private void Timer4Stop()
        {
            timer4.Stop();
            Interlocked.Exchange(ref inTimer4, 0);
        }

        private void Timer5Start()
        {
            timer5.Start();
        }
        private void Timer5Stop()
        {
            timer5.Stop();
            Interlocked.Exchange(ref inTimer5, 0);
        }

        #endregion


        public Main()
        {
            InitializeComponent();
#if DEBUG
            this.WindowState = FormWindowState.Normal;
#else
            this.WindowState = FormWindowState.Normal;
#endif
            //获取流程图标
            imageProcess = (Image)resources.GetObject("pictureBox6_Image");
            //设置数据库地址
            string dbPath = PubField.pathBase + "\\data.sqlite";
            SQLiteHelper.SetConnectionString(dbPath);
            //开机自启
            Tools.AutoStart(true);
            pushSettingMessage = SendSettingMsg;

        }

        private void Main_Load(object sender, EventArgs e)
        {
            //初始化
            TimerInit();
            //初始化参数
            Param.Init_Param(configfileName);
            //初始化串口信息
            if (!SerialInit())
            {
                return;
            }
            //串口数据绑定事件
            bluetoothSerialPort.DataReceived += new SerialDataReceivedEventHandler(bluetoothSerialPort_DataReceived);
            gpsSerialPort.DataReceived += new SerialDataReceivedEventHandler(gpsSerialPort_DataReceived);

            //初始化控件
            Thread workThread = new Thread(new ThreadStart(Init));
            workThread.IsBackground = true;
            workThread.Start();

            //初始化通讯方式
            if (Param.CommunicateMode == "0")//MQTT通讯方式
            {
                Thread myThread = new Thread(new ThreadStart(MQTTServerInit));
                myThread.IsBackground = true;
                myThread.Start();
            }
            else if (Param.CommunicateMode == "1")//Socket通讯方式
            {
                Thread myThread = new Thread(new ThreadStart(SocketServerInit));
                myThread.IsBackground = true;
                myThread.Start();
            }

            //发送蓝牙数据
            serialPortCtrl.SendMsg(bluetoothSerialPort, "AT");

            //执行定时任务
            Timer3Start();
            Timer4Start();
            Timer5Start();
        }
        /// <summary>
        /// 初始化串口
        /// </summary>
        private bool SerialInit()
        {
            bool isSuccess = true;
            try
            {
                //主串口
                if (!string.IsNullOrEmpty(Param.SerialPort))
                {
                    if (mainSerialPort.IsOpen)
                    {
                        mainSerialPort.Close();
                    }
                    mainSerialPort.PortName = Param.SerialPort;
                    mainSerialPort.BaudRate = 115200;
                    mainSerialPort.ReceivedBytesThreshold = 1;
                    mainSerialPort.Open();
                }
                else
                {
                    isSuccess = false;
                    errorMessage = "未配置主串口通讯";
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "未配置主串口通讯");
                }

                if (!string.IsNullOrEmpty(Param.BluetoothPort))
                {
                    if (bluetoothSerialPort.IsOpen)
                    {
                        bluetoothSerialPort.Close();
                    }
                    bluetoothSerialPort.PortName = Param.BluetoothPort;
                    bluetoothSerialPort.BaudRate = 9600;
                    bluetoothSerialPort.ReceivedBytesThreshold = 1;
                    bluetoothSerialPort.Open();
                }
                else
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "未配置蓝牙串口通讯");
                }


                if (!string.IsNullOrEmpty(Param.GPSPort))
                {
                    if (gpsSerialPort.IsOpen)
                    {
                        gpsSerialPort.Close();
                    }
                    gpsSerialPort.PortName = Param.GPSPort;
                    gpsSerialPort.BaudRate = 9600;
                    gpsSerialPort.ReceivedBytesThreshold = 1;
                    gpsSerialPort.Open();
                }
                else
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "未配置GPS串口通讯");
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                //errorMessage = ex.Message;
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "串口初始化失败:" + ex.Message);
            }
            showError(errorMessage);
            return isSuccess;
        }

        private void Init()
        {
            this.Invoke(new EventHandler(delegate
            {
                setControlAvailable(false);
                txtDeviceCode.Text = Param.DeviceID;
                int nCommunicateMode = -1;
                int.TryParse(Param.CommunicateMode, out nCommunicateMode);
                cbCommunicateMode.SelectedIndex = nCommunicateMode;
                txtMQTTAddress.Text = Param.MQTTServerIP;
                txtMQTTPort.Text = Param.MQTTServerPort;
                txtSocketAddress.Text = Param.SocketServerIP;
                txtSocketPort.Text = Param.SocketServerPort;
                lblWorkMode.Text = string.Format("当前运行模式_{0}", Param.WorkMode == "0" ? "自动" : "定时");
                string currVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
                lblVersion.Text = string.Format("当前版本：V_{0}", currVersion);
            }));
        }

        /// <summary>
        /// 时间控件初始化
        /// </summary>
        private void TimerInit()
        {
            //主流程
            timer1.Elapsed += new ElapsedEventHandler(timer1_Elapsed);
            timer1.Interval = 1000;

            //定时
            timer2.Elapsed += new ElapsedEventHandler(timer2_Elapsed);
            timer2.Interval = 1000;

            //任务
            timer3.Elapsed += new ElapsedEventHandler(timer3_Elapsed);
            timer3.Interval = 5 * 1000;

            //定时运行程序
            timer4.Elapsed += new ElapsedEventHandler(timer4_Elapsed);
            timer4.Interval = 5 * 1000;

            //设备状态
            timer5.Elapsed += new ElapsedEventHandler(timer5_Elapsed);
            timer5.Interval = 10 * 1000;
        }

        /// <summary>
        /// 初始化Socket服务器
        /// </summary>
        private void SocketServerInit()
        {
            try
            {
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "首次连接服务器！");
                string serverIP = Param.SocketServerIP;
                int serverPort = Convert.ToInt32(Param.SocketServerPort);
                socketClient = new SocketClient(this, serverIP, serverPort);
                socketClient.connectToSever();//连接服务器
                //设置信息
                SendSettingMsg();
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }
        /// <summary>
        /// 初始化MQTT服务器
        /// </summary>
        private void MQTTServerInit()
        {
            try
            {
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "首次连接MQTT服务器！");

                MQTTModel mQTTModel = new MQTTModel()
                {
                    Address = Param.MQTTServerIP,
                    Port = Param.MQTTServerPort,
                    ClientID = Param.MQTTClientID,
                    Account = Param.MQTTAccount,
                    Password = Param.MQTTPassword
                };

                mqttClient = new MQTTClient(this, mQTTModel);
                mqttClient.BuildMqttClient();//实例化MQTT客户端
                mqttClient.MqttConnect();//连接服务器
                //设置信息
                SendSettingMsg();
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 蓝牙接收数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bluetoothSerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (locker)
            {
                try
                {
                    if (bluetoothSerialPort == null || !bluetoothSerialPort.IsOpen)
                    {
                        return;
                    }
                    Thread.Sleep(850);//延迟接收数据
                    int ilen = bluetoothSerialPort.BytesToRead;
                    byte[] readBytes = new byte[ilen];
                    bluetoothSerialPort.Read(readBytes, 0, ilen);
                    string receiveMessage = ASCIIEncoding.ASCII.GetString(readBytes);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "蓝牙接收指令:" + receiveMessage);
                    receiveMessage = receiveMessage.Replace("\r\n", "");
                    if (string.IsNullOrEmpty(receiveMessage))
                    {
                        return;
                    }
                    isReceiveBluetooth = true;
                    if (receiveMessage != "OK")
                    {
                        //执行指令
                        BluetoothModel receiveData = JsonConvert.DeserializeObject<BluetoothModel>(receiveMessage);
                        dealBluetoothData(receiveData.Func, receiveData.Message);
                    }
                }
                catch (Exception ex)
                {
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                }
            }
        }

        /// <summary>
        /// 处理蓝牙数据
        /// </summary>
        /// <param name="func"></param>
        /// <param name="data"></param>
        private void dealBluetoothData(int func, object data)
        {
            try
            {
                BluetoothModel bluetoothModel = null;
                DeviceParams deviceParams = null;
                MotorShaft motorShaft = null;
                string strServerIP = "";
                int nServerPort = 0;
                byte[] res = null;
                switch (func)
                {
                    case 200:
                        //异常信息
                        bluetoothModel = new BluetoothModel()
                        {
                            Func = 200,
                            Message = getErrorCode(errorMessage)
                        };
                        serialPortCtrl.SendMsg(bluetoothSerialPort, JsonConvert.SerializeObject(bluetoothModel));
                        break;
                    case 201:
                        int nCommunicateMode = int.Parse(Param.CommunicateMode);
                        if (nCommunicateMode == 0)//mqtt
                        {
                            strServerIP = Param.MQTTServerIP;
                            nServerPort = int.Parse(Param.MQTTServerPort);
                        }
                        else
                        {
                            strServerIP = Param.SocketServerIP;
                            nServerPort = int.Parse(Param.SocketServerPort);
                        }
                        deviceParams = new DeviceParams()
                        {
                            DeviceID = Param.DeviceID,
                            CommunicateMode = nCommunicateMode,
                            ServerIP = strServerIP,
                            ServerPort = nServerPort,
                            Action = step.ToString(),
                            WorkMode = int.Parse(Param.WorkMode),
                            CollectTime = int.Parse(Param.CollectTime),
                            WorkHour = int.Parse(Param.WorkHour),
                            WorkMinute = int.Parse(Param.WorkMinute),
                            ChooseImageCount = int.Parse(Param.ChooseImageCount)
                        };
                        bluetoothModel = new BluetoothModel()
                        {
                            Func = 201,
                            Message = deviceParams
                        };
                        serialPortCtrl.SendMsg(bluetoothSerialPort, JsonConvert.SerializeObject(bluetoothModel));
                        break;
                    case 202:
                        deviceParams = JsonConvert.DeserializeObject<DeviceParams>(data + "");
                        strServerIP = deviceParams.ServerIP;
                        nServerPort = deviceParams.ServerPort;
                        Param.Set_ConfigParm(Main.configfileName, "Config", "DeviceID", deviceParams.DeviceID);
                        Param.Set_ConfigParm(Main.configfileName, "Config", "CommunicateMode", deviceParams.CommunicateMode + "");
                        string oriServerIP = "";
                        string oriServerPort = "";
                        if (deviceParams.CommunicateMode == 0)
                        {
                            Param.Set_ConfigParm(Main.configfileName, "Config", "MQTTServerIP", strServerIP);
                            Param.Set_ConfigParm(Main.configfileName, "Config", "MQTTServerPort", nServerPort + "");

                            oriServerIP = Param.MQTTServerIP;
                            oriServerPort = Param.MQTTServerPort;
                        }
                        else
                        {
                            Param.Set_ConfigParm(Main.configfileName, "Config", "SocketServerIP", strServerIP);
                            Param.Set_ConfigParm(Main.configfileName, "Config", "SocketServerPort", nServerPort + "");

                            oriServerIP = Param.SocketServerIP;
                            oriServerPort = Param.SocketServerPort;
                        }
                        int nOriServerPort = 0;
                        int.TryParse(oriServerPort,out nOriServerPort);

                        Param.Set_ConfigParm(Main.configfileName, "Config", "WorkMode", deviceParams.WorkMode + "");
                        Param.Set_ConfigParm(Main.configfileName, "Config", "CollectTime", deviceParams.CollectTime + "");
                        Param.Set_ConfigParm(Main.configfileName, "Config", "WorkHour", deviceParams.WorkHour + "");
                        Param.Set_ConfigParm(Main.configfileName, "Config", "WorkMinute", deviceParams.WorkMinute + "");
                        Param.Set_ConfigParm(Main.configfileName, "Config", "ChooseImageCount", deviceParams.ChooseImageCount + "");

                        Param.CollectTime = deviceParams.CollectTime + "";
                        Param.WorkHour = deviceParams.WorkHour + "";
                        Param.WorkMinute = deviceParams.WorkMinute + "";
                        Param.ChooseImageCount = deviceParams.ChooseImageCount + "";

                        bluetoothModel = new BluetoothModel()
                        {
                            Func = 202,
                            Message = "success"
                        };
                        serialPortCtrl.SendMsg(bluetoothSerialPort, JsonConvert.SerializeObject(bluetoothModel));
                        if (deviceParams.DeviceID != Param.DeviceID || deviceParams.CommunicateMode + "" != Param.CommunicateMode || deviceParams.ServerIP != oriServerIP || deviceParams.ServerPort != nOriServerPort || deviceParams.WorkMode + "" != Param.WorkMode)
                        {
                            Tools.RestStart();
                        }
                        break;

                    case 300:
                        //风机控制(0关闭 1打开)
                        if (data + "" == "1")
                        {
                            res = OperaCommand(0x91, 800);
                        }
                        else
                        {
                            res = OperaCommand(0x94, 0);
                        }
                        if (res != null)
                        {
                            bluetoothModel = new BluetoothModel()
                            {
                                Func = 300,
                                Message = "success"
                            };
                            serialPortCtrl.SendMsg(bluetoothSerialPort, JsonConvert.SerializeObject(bluetoothModel));
                        }
                        break;
                    case 301:
                        motorShaft = JsonConvert.DeserializeObject<MotorShaft>(data + "");
                        if (motorShaft.Way == 1)
                        {
                            res = OperaCommand(0x12, motorShaft.Step);
                        }
                        else
                        {
                            res = OperaCommand(0x11, motorShaft.Step);
                        }
                        if (res != null)
                        {
                            bluetoothModel = new BluetoothModel()
                            {
                                Func = 301,
                                Message = "success"
                            };
                            serialPortCtrl.SendMsg(bluetoothSerialPort, JsonConvert.SerializeObject(bluetoothModel));
                        }
                        break;
                    case 302:
                        motorShaft = JsonConvert.DeserializeObject<MotorShaft>(data + "");
                        byte oper = 0x00;
                        switch (motorShaft.Way)
                        {
                            case 0: oper = 0x20; motorShaft.Step = 1; break;
                            case 1: oper = 0x22; break;
                            case 2: oper = 0x21; break;
                            case 3: oper = 0x23; motorShaft.Step = 2; break;
                        }

                        res = OperaCommand(oper, motorShaft.Step);
                        if (res != null)
                        {
                            bluetoothModel = new BluetoothModel()
                            {
                                Func = 302,
                                Message = "success"
                            };
                            serialPortCtrl.SendMsg(bluetoothSerialPort, JsonConvert.SerializeObject(bluetoothModel));
                        }
                        break;
                    case 303:
                        //风机控制(0关闭 1打开)
                        if (data + "" == "1")
                        {
                            res = OperaCommand(0x92, 800);
                        }
                        else
                        {
                            res = OperaCommand(0x95, 0);
                        }
                        if (res != null)
                        {
                            bluetoothModel = new BluetoothModel()
                            {
                                Func = 303,
                                Message = "success"
                            };
                            serialPortCtrl.SendMsg(bluetoothSerialPort, JsonConvert.SerializeObject(bluetoothModel));
                        }
                        break;
                    case 304:
                        //程序重启
                        Tools.RestStart();
                        break;

                }


            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// GPS接收数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gpsSerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                byte[] data = new byte[200];
                int length = gpsSerialPort.Read(data, 0, 200);
                string Read = Encoding.Default.GetString(data, 0, length);
                if (Read.Contains("$GPGLL"))
                {
                    string str = Read.Substring(Read.IndexOf("$GPGLL"));
                    string[] bby = str.Split(',');
                    if (bby.Count() > 5)
                    {

                        if (bby[1] == "")
                            return;
                        if (bby[3] == "")
                            return;
                        double dlat = Convert.ToDouble(bby[1]) * 0.01;
                        double dlon = Convert.ToDouble(bby[3]) * 0.01;
                        dlat = Math.Floor(dlat) + ((dlat - Math.Floor(dlat)) / 0.6);
                        dlon = Math.Floor(dlon) + ((dlon - Math.Floor(dlon)) / 0.6);

                        string message = string.Format("纬度:{0}\r\n经度:{1}", dlat, dlon);
                        lblLocation.Text = message;
                        gpsSerialPort.Close();
                        //发送位置信息
                        SendLocation(dlat, dlon);
                    }
                }


            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        ///// <summary>
        ///// 工作流程
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        private void timer1_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref inTimer1, 1) == 0)
            {
                errorMessage = "";
                if (isBand)
                {
                    errorMessage = "载玻带异常";
                    return;
                }

                if (step == 1 && !isX2)
                {
                    errorMessage = "限位X2异常";
                    return;
                }
                Timer1Stop();
                //当前流程
                setProcess();
                //当前位置
                SendCurrAction();
                switch (step)
                {
                    case 0:
                        //初始化
                        Initialize();
                        break;
                    case 1:
                        //采集孢子
                        CollectSpore();
                        break;
                    case 2:
                        //拍照
                        TakePhotos();
                        break;
                    case 3:
                        //上传数据
                        UploadData();
                        break;
                    case 4:
                        //流程结束-更新执行指令
                        TaskComplete();
                        Timer3Start();
                        break;
                }

                Interlocked.Exchange(ref inTimer1, 0);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer2_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref inTimer2, 1) == 0)
            {
                DateTime dateNowTime = DateTime.Now;
                this.Invoke(new EventHandler(delegate
                {
                    if (step == 1)
                    {
                        //初始化90秒时间
                        showMessage("设备初始化中:" + Tools.GetNowTimeSpanSec(executeTime.AddSeconds(120), dateNowTime) + " 秒");
                        if (dateNowTime > executeTime.AddSeconds(120))
                        {
                            showMessage("无数据");
                            Timer2Stop();
                            Timer1Start();
                        }
                    }
                    else if (step == 2)
                    {
                        //吸风时长
                        int nCollectTime = int.Parse(Param.CollectTime);
                        showMessage("收集时间倒计时:" + Tools.GetNowTimeSpanSec(executeTime.AddMinutes(nCollectTime), dateNowTime) + " 秒");
                        if (dateNowTime > executeTime.AddMinutes(nCollectTime))
                        {
                            showMessage("无数据");
                            OperaCommand(0x94, 0);
                            Timer1Start();
                            Timer2Stop();
                        }
                    }

                }));
                Interlocked.Exchange(ref inTimer2, 0);
            }

        }
        /// <summary>
        /// 定时执行任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer3_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref inTimer3, 1) == 0)
            {
                string strWorkMode = Param.WorkMode;
                if (Param.WorkMode == "0")
                {
                    //自动
                    step = 0;
                    Timer1Start();
                    Timer3Stop();
                }
                else if (Param.WorkMode == "1")
                {
                    //定时
                    DateTime currTime = DateTime.Now;
                    int nCurrTime = currTime.Hour * 60 + currTime.Minute;
                    string sql = "select * from Task where TaskDate=@TaskDate limit 1 offset 0 ";
                    SQLiteParameter[] parameters =
                    {
                       new SQLiteParameter("@TaskDate", DbType.String)
                    };
                    parameters[0].Value = currTime.ToString("yyyy-MM-dd");
                    DataTable dataTable = SQLiteHelper.ExecuteQuery(sql, parameters);
                    if (dataTable != null && dataTable.Rows.Count > 0)
                    {
                        DataRow dataRow = dataTable.Rows[0];
                        if (dataRow["IsRun"] + "" == "0")
                        {
                            int runHour = int.Parse(dataRow["RunHour"] + "");
                            int runMinute = int.Parse(dataRow["RunMinute"] + "");
                            int nRunTime = runHour * 60 + runMinute;
                            if (nCurrTime > nRunTime)
                            {
                                //强制执行
                                this.Invoke(new EventHandler(delegate
                                {
                                    lblWorkMode.Text = "当前运行模式_强制运行";
                                }));
                                step = 0;
                                Timer1Start();
                                Timer3Stop();
                            }
                        }
                    }
                    else
                    {

                        int nWorkTime = int.Parse(Param.WorkHour) * 60 + int.Parse(Param.WorkMinute);
                        if (nCurrTime == nWorkTime)
                        {
                            sql = "insert into Task (TaskDate,RunHour,RunMinute,IsRun)values(@TaskDate,@RunHour,@RunMinute,0)";
                            SQLiteParameter[] taskParams =
                            {
                               new SQLiteParameter("@TaskDate", DbType.String),
                               new SQLiteParameter("@RunHour", DbType.Int32),
                               new SQLiteParameter("@RunMinute", DbType.Int32)
                            };
                            taskParams[0].Value = currTime.ToString("yyyy-MM-dd");
                            taskParams[1].Value = currTime.Hour;
                            taskParams[2].Value = currTime.Minute;
                            SQLiteHelper.ExecuteNonQuery(sql, taskParams);
                            step = 0;
                            Timer1Start();
                            Timer3Stop();
                        }
                    }

                }
                Interlocked.Exchange(ref inTimer3, 0);
            }
        }

        /// <summary>
        /// 定时运行程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer4_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref inTimer4, 1) == 0)
            {

                string currTime = DateTime.Now.ToString("HH:mm");

                //定时关机
                if (Param.isWinRestart == "1")
                {
                    //定时重启
                    if (currTime == "00:00")
                    {
                        Tools.WinRestart();
                    }
                }
                //定时上传数据
                if (currTime == "05:00" || currTime == "15:00" || currTime == "23:00")
                {
                    UploadData(false);
                }

                //相机搜索
                if (step <= 2 && string.IsNullOrEmpty(errorMessage))
                {
                    //errorMessage = "";
                    if (m_pMyCamera == null || !m_pMyCamera.MV_CC_IsDeviceConnected_NET())
                    {
                        if (!SearchDev())
                        {
                            errorMessage = "未搜索到相机";
                        }
                        else
                        {
                            if (!OpenDev())
                            {
                                errorMessage = "打开相机失败";
                            }
                            else
                            {
                                errorMessage = "";
                            }
                        }
                    }
                }
                showError(errorMessage);
                Interlocked.Exchange(ref inTimer4, 0);
            }
        }

        /// <summary>
        /// 发送设备当前状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer5_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref inTimer5, 1) == 0)
            {
                //判断是否联网
                bool isOnline = false;
                int flag = 0;
                if (Win32API.InternetGetConnectedState(out flag, 0))
                {
                    isOnline = true;
                }

                bool isFault = false;
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    isFault = true;
                }
                string str2Hex = string.Format("000{2}{1}{0}00", isFault == true ? 1 : 0, isReceiveBluetooth == false ? 1 : 0, isOnline == false ? 1 : 0);
                byte[] res = OperaCommand(0xA0, Convert.ToInt32(str2Hex, 2));
                if (res != null && res[2] == 0xA0)
                {
                    int dirs = res[6];
                    if (((dirs >> 0) & 0x01) == 1)
                    {
                        isX1 = true;
                    }
                    if (((dirs >> 1) & 0x01) == 1)
                    {
                        isX2 = true;
                    }
                    if (((dirs >> 5) & 0x01) == 1)
                    {
                        isAlarm = true;
                    }
                    if (((dirs >> 7) & 0x01) == 1)
                    {
                        isBand = true;
                    }

                    //发送设备异常信息
                    SendDeviceAbnormal(isBand, isAlarm);

                }
                Interlocked.Exchange(ref inTimer5, 0);
            }
        }


        /// <summary>
        /// 初始化
        /// </summary>
        private void Initialize()
        {
            //关闭相机
            CameraClose();
            executeTime = DateTime.Now;
            byte[] res = null;
            //发送运行位置
            res = OperaCommand(0x80, 1);
            if (res == null)
            {
                errorMessage = "主串口通讯异常";
                return;
            }
            //关闭风扇
            res = OperaCommand(0x96, 0);
            if (res == null)
            {
                errorMessage = "主串口通讯异常";
                return;
            }
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "关闭风扇");

            //关闭补光灯
            res = OperaCommand(0x95, 0);
            if (res == null)
            {
                errorMessage = "主串口通讯异常";
                return;
            }
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "关闭补光灯");
            //关闭吸风
            res = OperaCommand(0x94, 0);
            if (res == null)
            {
                errorMessage = "主串口通讯异常";
                return;
            }
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "关闭吸风");

            //相机到X2位置
            res = OperaCommand(0x23, 2);
            if (res == null)
            {
                errorMessage = "主串口通讯异常";
                return;
            }
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "相机到X2位置");

            //拉出载玻带（顺时针）
            int bandStep = int.Parse(Param.CollectStrength);
            res = OperaCommand(0x11, bandStep);
            if (res == null)
            {
                errorMessage = "主串口通讯异常";
                return;
            }
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, string.Format("拉出载玻带，步数为{0}", bandStep));
            step = 1;
            Timer2Start();
        }

        /// <summary>
        /// 收集孢子
        /// </summary>
        private void CollectSpore()
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "开始收集孢子");
            executeTime = DateTime.Now;
            byte[] res = null;
            res = OperaCommand(0x80, 2);
            if (res == null)
            {
                errorMessage = "主串口通讯异常";
                return;
            }
            //打开吸风
            res = OperaCommand(0x91, 800);
            if (res == null)
            {
                errorMessage = "主串口通讯异常";
                return;
            }
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "打开吸风");
            step = 2;
            Timer2Start();
        }

        /// <summary>
        /// 拍照
        /// </summary>
        private void TakePhotos()
        {
            //开启采集
            bool isStart = false;
            while (!isStart)
            {
                isStart = StartCollection(ref errorMessage);
                Thread.Sleep(2000);
            }
            OperaCommand(0x80, 3);
            //打开补光灯
            OperaCommand(0x92, 800);
            //开风扇
            OperaCommand(0x93, 0);
            //对焦（从限位X1开始直到限位X2，采集到合适图像及终止，如果直到限位X2都采集不到合适图像及终止）
            int focusCount = 0;
            int imageCount = 0;
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "开始对焦");
            while (focusCount <= int.Parse(Param.MaxFocusCount))
            {
                //移动轴二对焦
                OperaCommand(0x22, photoStep);
                Thread.Sleep(2000);
                DateTime currTime = DateTime.Now;
                Image image = GetPhoto();
                string imageName = currTime.ToString("yyyyMMddHHmmss") + ".jpg";
                string imagePath = Tools.SaveImage(image, imageName);
                if (string.IsNullOrEmpty(imagePath))
                {
                    continue;
                }
                //分析图像
                if (!ImageAnalysis(imagePath))
                {
                    Thread.Sleep(1000);
                    File.Delete(imagePath);
                }
                else
                {
                    string sql = "insert into Record(Flag,CollectTime)values(0,@CollectTime)";
                    SQLiteParameter[] parameters =
                    {
                        new SQLiteParameter("@CollectTime", DbType.String)
                    };
                    parameters[0].Value = currTime.ToString("yyyy-MM-dd HH:mm:ss");
                    SQLiteHelper.ExecuteNonQuery(sql, parameters);
                    imageCount++;
                    photoStep = 5;
                }

                focusCount++;
                showMessage(string.Format("相机第{0}次对焦", focusCount));
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, string.Format("相机第{0}次对焦", focusCount));
                if (imageCount >= int.Parse(Param.ChooseImageCount))
                {
                    break;
                }
            }

            step = 3;
            isX1 = false;
            isX2 = false;
            showMessage("无数据");
            CameraClose();
            //关闭补光灯
            OperaCommand(0x95, 0);
            //关闭风扇
            OperaCommand(0x96, 0);
            Timer1Start();
        }

        /// <summary>
        /// 上传数据
        /// </summary>
        private void UploadData(bool isProcess = true)
        {
            string sql = "select * from Record where Flag=0 ";
            DataTable dataTable = SQLiteHelper.ExecuteQuery(sql, null);
            if (dataTable == null || dataTable.Rows.Count <= 0)
            {
                return;
            }
            foreach (DataRow row in dataTable.Rows)
            {
                string strCollectTime = row["CollectTime"] + "";
                if (string.IsNullOrEmpty(strCollectTime))
                {
                    continue;
                }
                DateTime dateCollectTime = DateTime.Parse(strCollectTime);
                string imagePath = Param.basePath + "\\Images\\" + dateCollectTime.ToString("yyyyMMddHHmmss") + ".jpg";
                if (!File.Exists(imagePath))
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "文件不存在：" + imagePath);
                    continue;
                }
                //传输图像
                isTransferImage = false;
                bool bIsSuccess = SendPictureMsg(strCollectTime, imagePath);
                if (!bIsSuccess)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, string.Format("文件：{0}传输失败", imagePath));
                    break;
                }
            }
            if (isProcess)
            {
                step = 4;
                Timer1Start();
            }

        }

        /// <summary>
        /// 执行任务结束
        /// </summary>
        private void TaskComplete()
        {
            DateTime currTime = DateTime.Now;
            string sql = "update Task set IsRun=1 where TaskDate=@TaskDate";
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@TaskDate", DbType.String)
            };
            parameters[0].Value = currTime.ToString("yyyy-MM-dd");
            SQLiteHelper.ExecuteNonQuery(sql, parameters);
        }

        /// <summary>
        /// 计算主动轮带出制定长度载波带需要运转的步数
        /// </summary>
        /// <param name="length">载玻带长度</param>
        /// <returns></returns>
        //private int CalculationDrivingWheelSteps(int length)
        //{
        //    int nAccumulateSteps = int.Parse(Param.AccumulateSteps);
        //    int currWeeks = int.Parse((nAccumulateSteps / 6400.0f).ToString("F0")) + 1;//已经转动的周数
        //    double perimeter = 2 * Math.PI * (38.65 + currWeeks * 0.09f);//周长
        //    double step = (length / perimeter) * 6400.0f;
        //    return int.Parse(step.ToString("F0"));
        //}
        /// <summary>
        /// 发送通用成功信息
        /// </summary>
        /// <param name="func"></param>
        private void SendCommonMsg(int funCode, string message)
        {
            ProtocolModel model = new ProtocolModel()
            {
                func = funCode,
                devId = Param.DeviceID,
                err = "",
                message = ""
            };
            string jsonData = JsonConvert.SerializeObject(model);
            if (Param.CommunicateMode == "0")
            {
                if (mqttClient != null)
                    mqttClient.publishMessage(jsonData);
            }
            else
            {
                if (socketClient != null)
                    socketClient.SendMsg(jsonData);
            }
        }

        /// <summary>
        /// 发送当前动作
        /// </summary>
        private void SendCurrAction()
        {

            ProtocolModel model = new ProtocolModel()
            {
                func = 104,
                devId = Param.DeviceID,
                err = "",
                message = GetActionNameByStep()
            };

            string jsonData = JsonConvert.SerializeObject(model);
            if (Param.CommunicateMode == "0")
            {
                if (mqttClient != null)
                    mqttClient.publishMessage(jsonData);
            }
            else
            {
                if (socketClient != null)
                    socketClient.SendMsg(jsonData);
            }
        }

        /// <summary>
        /// 获取流程名称
        /// </summary>
        /// <returns></returns>
        private string GetActionNameByStep()
        {
            string currAction = "";
            switch (step)
            {
                case 0: currAction = "初始化"; break;
                case 1: currAction = "收集"; break;
                case 2: currAction = "拍照"; break;
                case 3: currAction = "上传数据"; break;
            }
            return currAction;
        }
        /// <summary>
        /// 获取设置信息
        /// </summary>
        /// <returns></returns>
        private void SendSettingMsg()
        {
            SettingInfo setting = new SettingInfo()
            {
                WorkMode = Param.WorkMode,
                CollectTime = Param.CollectTime,
                WorkHour = Param.WorkHour,
                WorkMinute = Param.WorkMinute,
                ChooseImageCount = Param.ChooseImageCount
            };

            ProtocolModel model = new ProtocolModel()
            {
                func = 200,
                devId = Param.DeviceID,
                err = "",
                message = setting
            };

            string jsonData = JsonConvert.SerializeObject(model);
            if (Param.CommunicateMode == "0")
            {
                if (mqttClient != null)
                    mqttClient.publishMessage(jsonData);
            }
            else
            {
                if (socketClient != null)
                    socketClient.SendMsg(jsonData);
            }

        }
        /// <summary>
        /// 发送位置信息
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        private void SendLocation(double lat, double lon)
        {
            ProtocolModel model = new ProtocolModel();
            model.devId = Param.DeviceID;
            model.func = 102;
            model.err = "";
            Location location = new Location()
            {
                lat = lat,
                lon = lon
            };
            model.message = location;
            string jsonData = JsonConvert.SerializeObject(model);
            if (Param.CommunicateMode == "0")
            {
                if (mqttClient != null)
                    mqttClient.publishMessage(jsonData);
            }
            else
            {
                if (socketClient != null)
                    socketClient.SendMsg(jsonData);
            }
        }

        /// <summary>
        /// 发送设备异常信息
        /// </summary>
        /// <param name="isBand"></param>
        /// <param name="isAlarm"></param>
        private void SendDeviceAbnormal(bool isBand, bool isAlarm)
        {

            ProtocolModel model = new ProtocolModel();
            model.devId = Param.DeviceID;
            model.func = 105;
            model.err = "";
            DeviceAbnormal deviceAbnormal = new DeviceAbnormal()
            {
                isAlarm = isAlarm,
                isBand = isBand
            };
            model.message = deviceAbnormal;
            string jsonData = JsonConvert.SerializeObject(model);
            if (Param.CommunicateMode == "0")
            {
                if (mqttClient != null)
                    mqttClient.publishMessage(jsonData);
            }
            else
            {
                if (socketClient != null)
                    socketClient.SendMsg(jsonData);
            }
        }
        /// <summary>
        /// 发送采集数据
        /// </summary>
        private bool SendPictureMsg(string collectTime, string path)
        {
            try
            {
                string picAliOssUrl = Tools.UploadImageAliOSS(path);
                if (string.IsNullOrEmpty(picAliOssUrl))
                {
                    return false;
                }

                ProtocolModel model = new ProtocolModel();
                model.devId = Param.DeviceID;
                model.func = 101;
                model.err = "";
                CollectInfo collect = new CollectInfo()
                {
                    collectTime = collectTime,
                    picUrl = picAliOssUrl
                };
                model.message = collect;
                string jsonData = JsonConvert.SerializeObject(model);

                DateTime startTime = DateTime.Now;
                if (Param.CommunicateMode == "0")
                {
                    if (mqttClient != null)
                        mqttClient.publishMessage(jsonData);
                }
                else
                {
                    if (socketClient != null)
                        socketClient.SendMsg(jsonData);
                }
                bool bIsSuccess = true;
                while (!isTransferImage)
                {
                    if (startTime.AddMinutes(2) < DateTime.Now)
                    {
                        bIsSuccess = false;//代表本张图像上传失败
                        break;
                    }
                    Thread.Sleep(1000);
                }
                if (!bIsSuccess)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "图像 " + Path.GetFileName(path) + " 未收到回应，本次发送将被终止，图像路径为:  " + path);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="jsonText"></param>
        public void DealMsg(string jsonText)
        {
            int func = -1;
            try
            {
                if (string.IsNullOrEmpty(jsonText))
                {
                    return;
                }
                ProtocolModel protocol = JsonConvert.DeserializeObject<ProtocolModel>(jsonText);
                if (protocol == null || string.IsNullOrEmpty(protocol.devId) || protocol.devId != Param.DeviceID)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "接收到的数据不合法！数据：" + jsonText);
                    return;
                }
                func = protocol.func;
                if (func == 100)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.KeepAliveLog, "接收数据:" + jsonText);
                }
                else
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "接收数据:" + jsonText);
                }
                switch (func)
                {
                    case 101:
                        //采集数据（更新状态）
                        string sql = "update Record set Flag=1 where CollectTime=@CollectTime";
                        SQLiteParameter[] parameters =
                        {
                           new SQLiteParameter("@CollectTime", DbType.String)
                        };
                        parameters[0].Value = protocol.message;
                        SQLiteHelper.ExecuteNonQuery(sql, parameters);
                        isTransferImage = true;
                        break;
                    case 104:
                        //当前位置
                        break;
                    case 200:
                        //读取参数
                        SendSettingMsg();
                        break;
                    case 201:
                        //设置参数
                        if (!string.IsNullOrEmpty(protocol.message + ""))
                        {
                            SettingInfo settingInfo = JsonConvert.DeserializeObject<SettingInfo>(protocol.message + "");
                            Param.Set_ConfigParm(configfileName, "Config", "WorkMode", settingInfo.WorkMode);
                            Param.Set_ConfigParm(configfileName, "Config", "CollectTime", settingInfo.CollectTime);
                            Param.Set_ConfigParm(configfileName, "Config", "WorkHour", settingInfo.WorkHour);
                            Param.Set_ConfigParm(configfileName, "Config", "WorkMinute", settingInfo.WorkMinute);
                            Param.Set_ConfigParm(configfileName, "Config", "ChooseImageCount", settingInfo.ChooseImageCount);
                            if (settingInfo.WorkMode != Param.WorkMode)
                            {
                                SendCommonMsg(201, "");
                                Tools.RestStart();
                            }
                            Param.WorkMode = settingInfo.WorkMode;
                            Param.CollectTime = settingInfo.CollectTime;
                            Param.WorkHour = settingInfo.WorkHour;
                            Param.WorkMinute = settingInfo.WorkMinute;
                            Param.ChooseImageCount = settingInfo.ChooseImageCount;
                        }
                        break;
                }

            }
            catch (Exception ex)
            {
                if (func == -1)
                {
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "接收到的数据不合法！接收数据：" + jsonText + "\r\n错误信息：" + ex.ToString());
                }
                else
                {
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "功能码：" + func + " Err！\r\n接收数据：" + jsonText + "\r\n错误信息：" + ex.ToString());
                }
            }
        }

        /// <summary>
        /// 设置流程显示
        /// </summary>
        /// <param name="pictureBox"></param>
        private void setProcess()
        {
            foreach (Control control in gbProcess.Controls)
            {
                if (control is PictureBox)
                {
                    PictureBox picture = (PictureBox)control;
                    picture.Image = null;
                }
            }

            switch (step)
            {
                case 0: pb1.Image = imageProcess; break;
                case 1: pb2.Image = imageProcess; break;
                case 2: pb3.Image = imageProcess; break;
                case 3: pb4.Image = imageProcess; break;
            }
        }

        /// <summary>
        /// 获取错误码
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        private string getErrorCode(string errorMsg)
        {
            string errorCode = "";
            switch (errorMsg)
            {
                case "未配置主串口通讯": errorCode = "1001"; break;
                case "主串口通讯异常": errorCode = "1002"; break;
                case "载玻带异常": errorCode = "1003"; break;
                case "限位X2异常": errorCode = "1004"; break;
                case "未搜索到相机": errorCode = "1005"; break;
                case "打开相机失败": errorCode = "1006"; break;
            }
            return errorCode;
        }

        /// <summary>
        /// 设置控件是否可用
        /// </summary>
        /// <param name="bEnabled"></param>
        private void setControlAvailable(bool bEnabled)
        {
            txtDeviceCode.Enabled = bEnabled;
            cbCommunicateMode.Enabled = bEnabled;
            txtMQTTAddress.Enabled = bEnabled;
            txtMQTTPort.Enabled = bEnabled;
            txtSocketAddress.Enabled = bEnabled;
            txtSocketPort.Enabled = bEnabled;
        }

        /// <summary>
        /// 通讯方式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbCommunicateMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = cbCommunicateMode.SelectedIndex;
            switch (selectedIndex)
            {
                case 0: tabControl1.SelectedIndex = 0; break;
                case 1: tabControl1.SelectedIndex = 1; break;
            }
        }
        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnModify_Click(object sender, EventArgs e)
        {
            setControlAvailable(true);
        }
        /// <summary>
        /// 应用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnApply_Click(object sender, EventArgs e)
        {
            setControlAvailable(false);
            string strDeviceID = txtDeviceCode.Text;
            int nCommunicateMode = cbCommunicateMode.SelectedIndex;
            string strMQTTAddress = txtMQTTAddress.Text;
            string strMQTTPort = txtMQTTPort.Text;
            string strSocketAddress = txtSocketAddress.Text;
            string strSocketPort = txtSocketPort.Text;

            if (strDeviceID != Param.DeviceID || nCommunicateMode != int.Parse(Param.CommunicateMode) || strMQTTAddress != Param.MQTTServerIP || strMQTTPort != Param.MQTTServerPort || strSocketAddress != Param.SocketServerIP || strSocketPort != Param.SocketServerPort)
            {
                Param.Set_ConfigParm(configfileName, "Config", "DeviceID", strDeviceID);
                Param.Set_ConfigParm(configfileName, "Config", "CommunicateMode", nCommunicateMode.ToString());
                Param.Set_ConfigParm(configfileName, "Config", "MQTTServerIP", strMQTTAddress);
                Param.Set_ConfigParm(configfileName, "Config", "MQTTServerPort", strMQTTPort);
                Param.Set_ConfigParm(configfileName, "Config", "SocketServerIP", strSocketAddress);
                Param.Set_ConfigParm(configfileName, "Config", "SocketServerPort", strSocketPort);
                DialogResult dialogResult = MessageBox.Show("检测到您更改了系统关键性配置，将在系统重启之后生效。点击“确定”将立即重启本程序，点击“取消”请稍后手动重启！", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                if (dialogResult == DialogResult.OK)
                {
                    Tools.RestStart();
                }
            }
            else
            {
                MessageBox.Show("设置成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        #region 相机方法

        /// <summary>
        /// 搜索设备
        /// </summary>
        private bool SearchDev()
        {
            try
            {
                int nRet;
                // ch:创建设备列表 en:Create Device List
                cbDeviceList.Items.Clear();
                nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_pDeviceList);
                if (0 != nRet)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "未检索到相机，nRet：" + nRet);
                    return false;
                }

                // ch:在窗体列表中显示设备名 | en:Display device name in the form list
                for (int i = 0; i < m_pDeviceList.nDeviceNum; i++)
                {
                    MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                    if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                    {
                        IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
                        MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                        if (gigeInfo.chUserDefinedName != "")
                        {
                            cbDeviceList.Items.Add("GigE: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")");
                        }
                        else
                        {
                            cbDeviceList.Items.Add("GigE: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                        }
                    }
                    else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                    {
                        IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stUsb3VInfo, 0);
                        MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                        if (usbInfo.chUserDefinedName != "")
                        {
                            cbDeviceList.Items.Add("USB: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")");
                        }
                        else
                        {
                            cbDeviceList.Items.Add("USB: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")");
                        }
                    }
                }

                // ch:选择第一项 | en:Select the first item
                if (m_pDeviceList.nDeviceNum != 0)
                {
                    cbDeviceList.SelectedIndex = 0;
                }
                return true;
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "SeacehDev异常：" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 打开设备
        /// </summary>
        private bool OpenDev()
        {
            try
            {
                if (m_pDeviceList.nDeviceNum == 0 || cbDeviceList.SelectedIndex == -1)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "未检索到相机！");
                    return false;
                }
                int nRet = -1;
                // ch:获取选择的设备信息 | en:Get selected device information
                MyCamera.MV_CC_DEVICE_INFO device =
                    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[cbDeviceList.SelectedIndex],
                                                                  typeof(MyCamera.MV_CC_DEVICE_INFO));

                // ch:打开设备 | en:Open device
                if (null == m_pMyCamera)
                {
                    m_pMyCamera = new MyCamera();
                    if (null == m_pMyCamera)
                    {
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "创建相机对象失败!");
                        return false;
                    }
                }

                nRet = m_pMyCamera.MV_CC_CreateDevice_NET(ref device);
                if (MyCamera.MV_OK != nRet)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "创建设备对象失败，nRet：" + nRet);
                    m_pMyCamera = null;
                    return false;
                }

                nRet = m_pMyCamera.MV_CC_OpenDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    m_pMyCamera.MV_CC_DestroyDevice_NET();
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "相机启动失败，nRet：" + nRet);
                    m_pMyCamera = null;
                    return false;
                }

                // ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    int nPacketSize = m_pMyCamera.MV_CC_GetOptimalPacketSize_NET();
                    if (nPacketSize > 0)
                    {
                        nRet = m_pMyCamera.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                        if (nRet != MyCamera.MV_OK)
                        {
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "警告：设置数据包大小失败，nRet：" + nRet);
                        }
                    }
                    else
                    {
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "警告：获取数据包大小失败，nRet：" + nRet);
                    }
                }

                // ch:设置采集连续模式 | en:Set Continues Aquisition Mode
                m_pMyCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
                m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                return true;
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "OpenDev异常：" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 开始采集
        /// </summary>
        private bool StartCollection(ref string error)
        {
            string message = "";
            try
            {
                if (m_pMyCamera == null || !m_pMyCamera.MV_CC_IsDeviceConnected_NET())
                {
                    return false;
                }

                int nRet = -1;
                // ch:开始采集 | en:Start Grabbing
                nRet = m_pMyCamera.MV_CC_StartGrabbing_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    message = "相机开启采集失败，nRet：" + nRet;
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, message);
                    return false;
                }
                else
                {
                    m_bGrabbing = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 获取图像信息
        /// </summary>
        /// <returns></returns>
        private Image GetPhoto()
        {
            try
            {
                if (m_pMyCamera == null)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "相机对象为空");
                    return null;
                }
                int nRet;
                UInt32 nPayloadSize = 0;
                MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
                nRet = m_pMyCamera.MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
                if (MyCamera.MV_OK != nRet)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "获取PayloadSize失败，nRet：" + nRet);
                    return null;
                }
                nPayloadSize = stParam.nCurValue;
                if (nPayloadSize > m_nBufSizeForDriver)
                {
                    m_nBufSizeForDriver = nPayloadSize;
                    m_pBufForDriver = new byte[m_nBufSizeForDriver];

                    // ch:同时对保存图像的缓存做大小判断处理 | en:Determine the buffer size to save image
                    // ch:BMP图片大小：width * height * 3 + 2048(预留BMP头大小) | en:BMP image size: width * height * 3 + 2048 (Reserved for BMP header)
                    m_nBufSizeForSaveImage = m_nBufSizeForDriver * 3 + 2048;
                    m_pBufForSaveImage = new byte[m_nBufSizeForSaveImage];
                }

                IntPtr pData = Marshal.UnsafeAddrOfPinnedArrayElement(m_pBufForDriver, 0);
                MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

                // ch:超时获取一帧，超时时间为1秒 | en:Get one frame timeout, timeout is 1 sec
                //连续取两帧图像，取第二帧图像作为图片
                int count = 0;
                while (count < 2)
                {
                    nRet = m_pMyCamera.MV_CC_GetOneFrameTimeout_NET(pData, m_nBufSizeForDriver, ref stFrameInfo, 1000);
                    if (MyCamera.MV_OK != nRet)
                    {
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "无数据，nRet：" + nRet);
                        return null;
                    }
                    count++;
                }
                IntPtr pImage = Marshal.UnsafeAddrOfPinnedArrayElement(m_pBufForSaveImage, 0);

                MyCamera.MV_SAVE_IMAGE_PARAM_EX stSaveParam = new MyCamera.MV_SAVE_IMAGE_PARAM_EX();
                stSaveParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Jpeg;
                stSaveParam.enPixelType = stFrameInfo.enPixelType;
                stSaveParam.pData = pData;
                stSaveParam.nDataLen = stFrameInfo.nFrameLen;
                stSaveParam.nHeight = stFrameInfo.nHeight;
                stSaveParam.nWidth = stFrameInfo.nWidth;
                stSaveParam.pImageBuffer = pImage;
                stSaveParam.nBufferSize = m_nBufSizeForSaveImage;
                stSaveParam.nJpgQuality = 80;
                nRet = m_pMyCamera.MV_CC_SaveImageEx_NET(ref stSaveParam);
                if (MyCamera.MV_OK != nRet)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "保存失败，nRet：" + nRet);
                    return null;
                }
                MemoryStream ms = new MemoryStream(m_pBufForSaveImage);
                Image realTimeImage = Bitmap.FromStream(ms, true);
                return realTimeImage;
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "保存失败!  " + ex.ToString());
                return null;
            }

        }
        /// <summary>
        /// 图像分析
        /// </summary>
        /// <param name="path">图像路径</param>
        /// <returns>包围性状所占总面积</returns>
        private bool ImageAnalysis(string path)
        {
            bool isMeet = false;
            try
            {
                //原始图像
                Mat img = CvInvoke.Imread(path);
                //灰度化
                Mat gray = new Mat();
                CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);
                //大津法局部阀值二值化
                Mat dst = new Mat();
                CvInvoke.AdaptiveThreshold(gray, dst, 255, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 81, -1);
                //指定参数获得结构元素 形态学闭运算去噪
                Mat element = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(9, 9), new Point(3, 3));
                CvInvoke.MorphologyEx(dst, dst, MorphOp.Open, element, new Point(1, 1), 1, BorderType.Default, new MCvScalar(255, 0, 0, 255));
                //检测轮廓
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours(dst, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                //遍历包围性轮廓的最大长度 
                double are = 0;
                double count = 0;
                for (int i = 0; i < contours.Size; i++)
                {
                    //计算包围性状的面积 
                    are = CvInvoke.ContourArea(contours[i], false);
                    if (are < 3000/*过滤掉面积小于3000的*/)
                    {
                        continue;
                    }
                    count++;
                }
                contours.Dispose();
                element.Dispose();
                dst.Dispose();
                gray.Dispose();
                img.Dispose();
                if (count > 0)
                {
                    isMeet = true;
                }
                return isMeet;
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 关闭相机
        /// </summary>
        private void CameraClose()
        {
            if (m_bGrabbing)
            {
                m_bGrabbing = false;
                m_pMyCamera.MV_CC_StopGrabbing_NET();
            }
            if (m_pMyCamera != null)
            {
                m_pMyCamera.MV_CC_CloseDevice_NET();
                m_pMyCamera.MV_CC_DestroyDevice_NET();
                m_pMyCamera = null;
            }
        }

        #endregion

        #region 操作方法


        private byte[] OperaCommand(byte func, int value)
        {
            lock (cmdLocker)
            {
                try
                {
                    if (mainSerialPort == null || !mainSerialPort.IsOpen)
                        return null;
                    mainSerialPort.DiscardInBuffer();
                    byte[] cmd = { 0xBB, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    cmd[2] = func;
                    cmd[3] = (byte)((value >> 24) & 0xFF);
                    cmd[4] = (byte)((value >> 16) & 0xFF);
                    cmd[5] = (byte)((value >> 8) & 0xFF);
                    cmd[6] = (byte)((value >> 0) & 0xFF);
                    cmd[7] = GetCheckByte(cmd);
                    return serialPortCtrl.SendCommand(mainSerialPort, cmd);
                }
                catch (Exception ex)
                {
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                    return null;
                }
            }
        }


        /// <summary>
        /// 校验
        /// </summary>
        /// <param name="by"></param>
        /// <returns></returns>
        private byte GetCheckByte(byte[] cmd)
        {
            try
            {
                int size = cmd.Length;
                int crc = 0;
                for (int i = 1; i < size - 1; i++)
                    crc += cmd[i];
                return (byte)(crc & 0xFF);
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return 0;
            }

        }
        #endregion

        /// <summary>
        /// 错误信息
        /// </summary>
        /// <param name="message"></param>

        private void showError(string message)
        {
            if (lblError.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    lblError.Text = message;
                }));
            }
            else
            {
                lblError.Text = message;
            }
        }

        /// <summary>
        /// 显示信息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="error"></param>
        private void showMessage(string message)
        {
            if (message.Length > 15)
            {
                message = message.Substring(0, 15) + "\r\n" + message.Substring(15);
            }
            if (lblMessage.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    lblMessage.Text = message;
                }));
            }
            else
            {
                lblMessage.Text = message;
            }

        }



        /// <summary>
        /// 最小化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbMin_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        /// <summary>
        /// 关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbClose_Click(object sender, EventArgs e)
        {
            Timer1Stop();
            Timer2Stop();
            Timer3Stop();
            Timer4Stop();
            Timer5Stop();
            if (Param.CommunicateMode == "0")
            {
                if (mqttClient != null)
                    mqttClient.CloseMQTT();
            }
            else
            {
                if (socketClient != null)
                    socketClient.CloseSocket();
            }

            if (mainSerialPort.IsOpen)
                mainSerialPort.Close();

            if (bluetoothSerialPort.IsOpen)
                bluetoothSerialPort.Close();

            if (gpsSerialPort.IsOpen)
                gpsSerialPort.Close();

            //关闭相机
            CameraClose();

            System.Environment.Exit(0);
        }

        //窗口移动
        Point mPoint;

        private void panelTitle_MouseDown(object sender, MouseEventArgs e)
        {
            mPoint = new Point(e.X, e.Y);
        }

        private void panelTitle_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Location = new Point(this.Location.X + e.X - mPoint.X, this.Location.Y + e.Y - mPoint.Y);
            }
        }
        /// <summary>
        /// 系统参数设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbSetting_Click(object sender, EventArgs e)
        {
            Setting setting = new Setting();
            setting.ShowDialog();
        }

        private void btnUploadData_Click(object sender, EventArgs e)
        {
            UploadData(false);
        }

    }
}
