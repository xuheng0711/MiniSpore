using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using MiniSpore.Common;
using MiniSpore.Model;
using MvCamCtrl.NET;
using MvCamCtrl.NET.CameraParams;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Resources;
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
        //位置标记
        private int step = -1;
        ////配置文件地址
        internal static string configfileName = "Config.ini";
        ResourceManager resources = new ResourceManager("MiniSpore.Properties.Resources", typeof(Main).Assembly);
        private Image imageProcess = null;
        //程序锁
        private static readonly Object locker = new object();
        //Socket服务器
        public SocketClient socketClient = null;
        //MQTT服务器
        public MQTTClient mqttClient = null;

        #region 相机对象
        private CCamera m_MyCamera = null;//相机对象
        ComboBox cbDeviceList = new ComboBox();//设备列表
        List<CCameraInfo> m_ltDeviceList = new List<CCameraInfo>();
        bool m_bGrabbing = false;//是否采集
        #endregion

        //传输图像是否完成
        private bool isTransferImage = false;

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

        #endregion


        public Main()
        {
            InitializeComponent();
#if DEBUG
            this.WindowState = FormWindowState.Normal;
#else
            this.WindowState = FormWindowState.Maximized;
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
            //执行定时任务
            Timer3Start();
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

        ///// <summary>
        ///// 工作流程
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        private void timer1_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref inTimer1, 1) == 0)
            {
                switch (step)
                {
                    case 0:
                        //初始化
                        Initialize(); break;
                    case 1:
                        //采集孢子
                        CollectSpore(); break;
                    case 2:
                        //拍照
                        TakePhotos(); break;
                    case 3:
                        //上传数据
                        UploadData(); break;
                    case 4:
                        //流程结束-更新执行指令
                        TaskComplete(); break;
                }
                //当前位置
                SendCurrAction();
                Timer1Stop();
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
                                lblWorkMode.Text = "当前运行模式_强制运行";
                                Timer1Start();
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
                            Timer1Start();
                        }
                    }

                }
                Interlocked.Exchange(ref inTimer3, 0);
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Initialize()
        {
            //关闭补光灯

            //关闭吸风

            //相机到初始位置

            //拉出载玻带

        }

        /// <summary>
        /// 收集孢子
        /// </summary>
        private void CollectSpore()
        {
            //打开吸风


        }

        /// <summary>
        /// 拍照
        /// </summary>
        private void TakePhotos()
        {

            //调焦

            //拍照

        }

        /// <summary>
        /// 上传数据
        /// </summary>
        private void UploadData()
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
                bool bIsSuccess = SendPictureMsg(strCollectTime, imagePath);
                if (!bIsSuccess)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, string.Format("文件：{0}传输失败", imagePath));
                    break;
                }
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
        /// 发送当前动作
        /// </summary>
        private void SendCurrAction()
        {
            string currAction = "";
            switch (step)
            {
                case 0: currAction = "初始化"; break;
                case 1: currAction = "收集"; break;
                case 2: currAction = "拍照"; break;
                case 3: currAction = "上传数据"; break;
            }
            ProtocolModel model = new ProtocolModel()
            {
                func = 200,
                devId = Param.DeviceID,
                err = "",
                message = currAction
            };

            string jsonData = JsonConvert.SerializeObject(model);
            if (Param.CommunicateMode == "0")
            {
                mqttClient.publishMessage(jsonData);
            }
            else
            {
                socketClient.SendMsg(jsonData);
            }
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
                WorkMinute = Param.WorkMinute
            };

            ProtocolModel model = new ProtocolModel()
            {
                func = 300,
                devId = Param.DeviceID,
                err = "",
                message = setting
            };

            string jsonData = JsonConvert.SerializeObject(model);
            if (Param.CommunicateMode == "0")
            {
                mqttClient.publishMessage(jsonData);
            }
            else
            {
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
                    mqttClient.publishMessage(jsonData);
                }
                else
                {
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
                string message = "";
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
                    case 200:
                        //获取参数



                        break;
                    case 201:
                        //设置参数

                        break;
                }

                if (Param.CommunicateMode == "0")
                {
                    //MQTT


                }
                else
                {
                    //Socket


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
        private void setProcess(PictureBox pictureBox)
        {
            foreach (Control control in gbProcess.Controls)
            {
                if (control is PictureBox)
                {
                    PictureBox picture = (PictureBox)control;
                    picture.Image = null;
                }
            }
            pictureBox.Image = imageProcess;
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
                Param.Set_ConfigParm(Main.configfileName, "Config", "DeviceID", strDeviceID);
                Param.Set_ConfigParm(Main.configfileName, "Config", "CommunicateMode", nCommunicateMode.ToString());
                Param.Set_ConfigParm(Main.configfileName, "Config", "MQTTServerIP", strMQTTAddress);
                Param.Set_ConfigParm(Main.configfileName, "Config", "MQTTServerPort", strMQTTPort);
                Param.Set_ConfigParm(Main.configfileName, "Config", "SocketServerIP", strSocketAddress);
                Param.Set_ConfigParm(Main.configfileName, "Config", "SocketServerPort", strSocketPort);
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
                cbDeviceList.Items.Clear();
                m_ltDeviceList.Clear();
                int nRet = CSystem.EnumDevices(CSystem.MV_GIGE_DEVICE | CSystem.MV_USB_DEVICE, ref m_ltDeviceList);
                if (0 != nRet)
                {
                    return false;
                }
                // ch:在窗体列表中显示设备名 | en:Display device name in the form list
                for (int i = 0; i < m_ltDeviceList.Count; i++)
                {
                    if (m_ltDeviceList[i].nTLayerType == CSystem.MV_GIGE_DEVICE)
                    {
                        CGigECameraInfo gigeInfo = (CGigECameraInfo)m_ltDeviceList[i];

                        if (gigeInfo.UserDefinedName != "")
                        {
                            cbDeviceList.Items.Add("GEV: " + gigeInfo.UserDefinedName + " (" + gigeInfo.chSerialNumber + ")");
                        }
                        else
                        {
                            cbDeviceList.Items.Add("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                        }
                    }
                    else if (m_ltDeviceList[i].nTLayerType == CSystem.MV_USB_DEVICE)
                    {
                        CUSBCameraInfo usbInfo = (CUSBCameraInfo)m_ltDeviceList[i];
                        if (usbInfo.UserDefinedName != "")
                        {
                            cbDeviceList.Items.Add("U3V: " + usbInfo.UserDefinedName + " (" + usbInfo.chSerialNumber + ")");
                        }
                        else
                        {
                            cbDeviceList.Items.Add("U3V: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")");
                        }
                    }
                }

                // ch:选择第一项 | en:Select the first item
                if (m_ltDeviceList.Count == 0)
                {
                    return false;
                }
                cbDeviceList.SelectedIndex = 0;
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
                if (m_ltDeviceList.Count == 0 || cbDeviceList.SelectedIndex == -1)
                {
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "未检索到相机");
                    return false;
                }

                // ch:获取选择的设备信息 | en:Get selected device information
                CCameraInfo device = m_ltDeviceList[cbDeviceList.SelectedIndex];
                // ch:打开设备 | en:Open device
                if (null == m_MyCamera)
                {
                    m_MyCamera = new CCamera();
                    if (null == m_MyCamera)
                    {
                        return false;
                    }
                }

                int nRet = m_MyCamera.CreateHandle(ref device);
                if (CErrorDefine.MV_OK != nRet)
                {
                    return false;
                }

                nRet = m_MyCamera.OpenDevice();
                if (CErrorDefine.MV_OK != nRet)
                {
                    m_MyCamera.DestroyHandle();
                    return false;
                }

                // ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)
                if (device.nTLayerType == CSystem.MV_GIGE_DEVICE)
                {
                    int nPacketSize = m_MyCamera.GIGE_GetOptimalPacketSize();
                    if (nPacketSize > 0)
                    {
                        nRet = m_MyCamera.SetIntValue("GevSCPSPacketSize", (uint)nPacketSize);
                        if (nRet != CErrorDefine.MV_OK)
                        {
                            DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "设置网络最佳包大小失败：" + nRet);
                        }
                    }
                    else
                    {
                        DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "获取网络最佳包大小失败：" + nPacketSize);
                    }
                }

                // ch:设置采集连续模式 | en:Set Continues Aquisition Mode
                m_MyCamera.SetEnumValue("AcquisitionMode", (uint)MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_SINGLE);
                m_MyCamera.SetEnumValue("TriggerMode", (uint)MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
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
        private void StartCollection()
        {
            try
            {
                string message = "";
                if (m_MyCamera == null || !m_MyCamera.IsDeviceConnected())
                {
                    return;
                }
                int nRet = -1;
                // ch:开始采集 | en:Start Grabbing
                nRet = m_MyCamera.StartGrabbing();
                if (CErrorDefine.MV_OK != nRet)
                {
                    CameraClose();
                    message = "相机开启采集失败，nRet：" + nRet;
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, message);
                    return;
                }
                else
                {
                    m_bGrabbing = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                m_MyCamera.StopGrabbing();
            }
            if (m_MyCamera != null)
            {
                m_MyCamera.CloseDevice();
                m_MyCamera.DestroyHandle();
                m_MyCamera = null;
            }
        }

        #endregion

        /// <summary>
        /// 图像分析
        /// </summary>
        /// <param name="path">图像路径</param>
        /// <returns>包围性状所占总面积</returns>
        private double ImageAnalysis(string path)
        {
            try
            {
                //原始图像
                Mat img = CvInvoke.Imread(path);
                //灰度化
                Mat gray = new Mat();
                CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);
                //大津法局部阀值二值化
                Mat dst = new Mat();
                CvInvoke.AdaptiveThreshold(gray, dst, 255, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 101, -1);
                //指定参数获得结构元素 形态学闭运算去噪
                Mat element = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(9, 9), new Point(3, 3));
                CvInvoke.MorphologyEx(dst, dst, MorphOp.Open, element, new Point(1, 1), 1, BorderType.Default, new MCvScalar(255, 0, 0, 255));
                //检测轮廓
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours(dst, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                //遍历包围性轮廓的最大长度 
                double are = 0;
                double ares = 0;
                double count = 0;
                //VectorOfPoint vp = new VectorOfPoint();
                for (int i = 0; i < contours.Size; i++)
                {
                    //计算包围性状的面积 
                    are = CvInvoke.ContourArea(contours[i], false);
                    if (are < 3000/*过滤掉面积小于3000的*/)
                    {
                        continue;
                    }
                    count++;
                    ares += are;
                }
                contours.Dispose();
                element.Dispose();
                dst.Dispose();
                gray.Dispose();
                img.Dispose();
                return ares;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return 0;
            }
        }

        /// <summary>
        /// 显示信息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="error"></param>
        private void showMessage(string message, bool error = false)
        {
            lock (locker)
            {
                this.Invoke(new EventHandler(delegate
                {
                    if (error)
                    {
                        lblError.Text = message;
                    }
                    else
                    {
                        lblMessage.Text = message;
                    }
                }));
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
            if (Param.CommunicateMode == "0")
            {
                mqttClient.CloseMQTT();
            }
            else
            {
                socketClient.CloseSocket();
            }
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
            DialogResult dialogResult = setting.ShowDialog();
            if (dialogResult == DialogResult.Yes)
            {

            }
        }


    }
}
