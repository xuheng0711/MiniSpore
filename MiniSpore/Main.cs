using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using MiniSpore.Common;
using MvCamCtrl.NET;
using MvCamCtrl.NET.CameraParams;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MiniSpore
{
    public partial class Main : Form
    {
        ResourceManager resources = new ResourceManager("MiniSpore.Properties.Resources", typeof(Main).Assembly);
        private Image imageProcess = null;

        #region 相机对象
        private CCamera m_MyCamera = null;//相机对象
        ComboBox cbDeviceList = new ComboBox();//设备列表
        List<CCameraInfo> m_ltDeviceList = new List<CCameraInfo>();
        bool m_bGrabbing = false;//是否采集
        #endregion

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            //获取流程图标
            imageProcess = (Image)resources.GetObject("pictureBox6_Image");
            //初始化控件
            Thread workThread = new Thread(new ThreadStart(Init));
            workThread.IsBackground = true;
            workThread.Start();

        }


        private void Init()
        {
            this.Invoke(new EventHandler(delegate
            {
                setControlAvailable(false);
                string currVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
                lblVersion.Text = string.Format("当前版本：V_{0}", currVersion);
            }));

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
        }

        #region 相机方法
        /// <summary>
        /// 搜索设备
        /// </summary>
        private void SeacehDev()
        {
            try
            {
                cbDeviceList.Items.Clear();
                m_ltDeviceList.Clear();
                int nRet = CSystem.EnumDevices(CSystem.MV_GIGE_DEVICE | CSystem.MV_USB_DEVICE, ref m_ltDeviceList);
                if (0 != nRet)
                {
                    return;
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
                if (m_ltDeviceList.Count != 0)
                {
                    cbDeviceList.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "SeacehDev异常：" + ex.Message);
            }
        }

        /// <summary>
        /// 打开设备
        /// </summary>
        private void OpenDev()
        {
            try
            {
                if (m_ltDeviceList.Count == 0 || cbDeviceList.SelectedIndex == -1)
                {
                    MessageBox.Show("未检索到相机！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ch:获取选择的设备信息 | en:Get selected device information
                CCameraInfo device = m_ltDeviceList[cbDeviceList.SelectedIndex];
                // ch:打开设备 | en:Open device
                if (null == m_MyCamera)
                {
                    m_MyCamera = new CCamera();
                    if (null == m_MyCamera)
                    {
                        return;
                    }
                }

                int nRet = m_MyCamera.CreateHandle(ref device);
                if (CErrorDefine.MV_OK != nRet)
                {
                    return;
                }

                nRet = m_MyCamera.OpenDevice();
                if (CErrorDefine.MV_OK != nRet)
                {
                    m_MyCamera.DestroyHandle();
                    return;
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
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "OpenDev异常：" + ex.Message);
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

        }


    }
}
