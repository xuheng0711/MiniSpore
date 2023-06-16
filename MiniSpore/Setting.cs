using MiniSpore.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MiniSpore
{
    public partial class Setting : Form
    {
        public Setting()
        {
            InitializeComponent();
        }

        private void Setting_Load(object sender, EventArgs e)
        {
            initControl();
            //初始化串口
            loadOpenPortNames();
            //读取数据
            readData();
        }

        /// <summary>
        /// 初始化控件
        /// </summary>
        private void initControl()
        {
            cbTimeSlot1.Items.Clear();
            cbTimeSlot2.Items.Clear();
            cbTimeSlot3.Items.Clear();
            for (int i = 0; i < 24; i++)
            {
                cbTimeSlot1.Items.Add(i);
                cbTimeSlot2.Items.Add(i);
                cbTimeSlot3.Items.Add(i);
            }
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        private void readData()
        {
            cbPort.SelectedItem = Param.SerialPort;
            cbBluetoothPort.SelectedItem = Param.BluetoothPort;
            cbGPSPort.SelectedItem = Param.GPSPort;
            int nWorkMode = 1;
            if (!string.IsNullOrEmpty(Param.WorkMode))
            {
                int.TryParse(Param.WorkMode, out nWorkMode);
            }
            cbWorkMode.SelectedIndex = nWorkMode;
            tbCollectTime.Text = Param.CollectTime;
            tbChooseImageCount.Text = Param.ChooseImageCount;
            cbTimeSlot1.SelectedItem = string.IsNullOrEmpty(Param.TimeSlot1) == true ? -1 : int.Parse(Param.TimeSlot1);
            cbTimeSlot2.SelectedItem = string.IsNullOrEmpty(Param.TimeSlot2) == true ? -1 : int.Parse(Param.TimeSlot2);
            cbTimeSlot3.SelectedItem = string.IsNullOrEmpty(Param.TimeSlot3) == true ? -1 : int.Parse(Param.TimeSlot3);
        }

        /// <summary>
        /// 加载可用串口
        /// </summary>
        private void loadOpenPortNames()
        {
            String[] Portname = SerialPort.GetPortNames();
            foreach (string str in Portname)
            {
                cbPort.Items.Add(str);
                cbGPSPort.Items.Add(str);
                cbBluetoothPort.Items.Add(str);
            }
            cbPort.SelectedIndex = -1;
        }

        /// <summary>
        /// 保存参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, EventArgs e)
        {
            string strPort = cbPort.SelectedItem + "";
            string strBluetoothPort = cbBluetoothPort.SelectedItem + "";
            string strGPSPort = cbGPSPort.SelectedItem + "";
            int nWorkMode = cbWorkMode.SelectedIndex;
            string strCollectTime = tbCollectTime.Text.Trim();
            string strChooseImageCount = tbChooseImageCount.Text.Trim();
            string strTimeSlot1 = cbTimeSlot1.SelectedItem + "";
            string strTimeSlot2 = cbTimeSlot2.SelectedItem + "";
            string strTimeSlot3 = cbTimeSlot3.SelectedItem + "";
            Param.Set_ConfigParm(Main.configfileName, "Config", "ChooseImageCount", strChooseImageCount);

            if (nWorkMode != int.Parse(Param.WorkMode))
            {
                byte[] res = Main.pushWorkMode(nWorkMode.ToString(), false);
                if (res == null || res.Length != 8 || res[5] != 0x00)
                {
                    MessageBox.Show("工作模式切换失败，请重试", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (strCollectTime != Param.CollectTime || strTimeSlot1 != Param.TimeSlot1 || strTimeSlot2 != Param.TimeSlot2 || strTimeSlot3 != Param.TimeSlot3)
            {
                if (!Main.pushTimeSlot(strCollectTime, strTimeSlot1, strTimeSlot2, strTimeSlot3))
                {
                    MessageBox.Show("采集时间点设置失败，请重试", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Param.CollectTime = strCollectTime;
                Param.TimeSlot1 = strTimeSlot1;
                Param.TimeSlot2 = strTimeSlot2;
                Param.TimeSlot3 = strTimeSlot3;
                Param.Set_ConfigParm(Main.configfileName, "Config", "CollectTime", strCollectTime);
                Param.Set_ConfigParm(Main.configfileName, "Config", "TimeSlot1", strTimeSlot1);
                Param.Set_ConfigParm(Main.configfileName, "Config", "TimeSlot2", strTimeSlot2);
                Param.Set_ConfigParm(Main.configfileName, "Config", "TimeSlot3", strTimeSlot3);
            }


            if (strPort != Param.SerialPort || strBluetoothPort != Param.BluetoothPort || strGPSPort != Param.GPSPort || nWorkMode != int.Parse(Param.WorkMode))
            {
                Param.Set_ConfigParm(Main.configfileName, "Config", "SerialPort", strPort);
                Param.Set_ConfigParm(Main.configfileName, "Config", "BluetoothPort", strBluetoothPort);
                Param.Set_ConfigParm(Main.configfileName, "Config", "GPSPort", strGPSPort);
                Param.Set_ConfigParm(Main.configfileName, "Config", "WorkMode", nWorkMode.ToString());
                Param.Set_ConfigParm(Main.configfileName, "Config", "SerialPort", strPort);
                DialogResult dialogResult = MessageBox.Show("检测到您更改了系统关键性配置，将在系统重启之后生效。点击“确定”将立即重启本程序，点击“取消”请稍后手动重启！", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                if (dialogResult == DialogResult.OK)
                {
                    Tools.RestStart();
                }
            }
            else
            {

                Param.ChooseImageCount = strChooseImageCount;
                Main.pushSettingMessage();
                MessageBox.Show("设置成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void pbClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Yes;
            this.Close();
        }


    }
}
