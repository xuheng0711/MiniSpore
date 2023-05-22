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
            //初始化串口
            loadOpenPortNames();
            //读取数据
            readData();
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        private void readData()
        {
            cbPort.SelectedItem = Param.SerialPort;
            cbBaudrate.SelectedItem = Param.Baudrate;
            cbBluetoothPort.SelectedItem = Param.BluetoothPort;
            cbGPSPort.SelectedItem = Param.GPSPort;
            int nWorkMode = -1;
            int.TryParse(Param.WorkMode, out nWorkMode);
            cbWorkMode.SelectedIndex = nWorkMode;
            tbCollectTime.Text = Param.CollectTime;
            tbCollectHour.Text = Param.WorkHour;
            tbCollectMinute.Text = Param.WorkMinute;
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

        private void btnSave_Click(object sender, EventArgs e)
        {
            string strPort = cbPort.SelectedItem + "";
            string strBaudrate = cbBaudrate.SelectedItem + "";
            string strBluetoothPort = cbBluetoothPort.SelectedItem + "";
            string strGPSPort = cbGPSPort.SelectedItem + "";
            int nWorkMode = cbWorkMode.SelectedIndex;
            string strCollectTime = tbCollectTime.Text.Trim();
            string strCollectHour = tbCollectHour.Text.Trim();
            string strCollectMinute = tbCollectMinute.Text.Trim();

            Param.Set_ConfigParm(Main.configfileName, "Config", "CollectTime", strCollectTime);
            Param.Set_ConfigParm(Main.configfileName, "Config", "CollectHour", strCollectHour);
            Param.Set_ConfigParm(Main.configfileName, "Config", "CollectMinute", strCollectMinute);
            if (strPort != Param.SerialPort || strBaudrate != Param.Baudrate || strBluetoothPort != Param.BluetoothPort || strGPSPort != Param.GPSPort || nWorkMode != int.Parse(Param.WorkMode))
            {
                Param.Set_ConfigParm(Main.configfileName, "Config", "SerialPort", strPort);
                Param.Set_ConfigParm(Main.configfileName, "Config", "BluetoothPort", strBluetoothPort);
                Param.Set_ConfigParm(Main.configfileName, "Config", "GPSPort", strGPSPort);
                Param.Set_ConfigParm(Main.configfileName, "Config", "Baudrate", strBaudrate);
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
                Param.CollectTime = strCollectTime;
                Param.WorkHour = strCollectHour;
                Param.WorkMinute = strCollectMinute;
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
