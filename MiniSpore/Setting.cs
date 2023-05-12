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
            }
            cbPort.SelectedIndex = -1;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {


        }

        private void pbClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Yes;
            this.Close();
        }

    
    }
}
