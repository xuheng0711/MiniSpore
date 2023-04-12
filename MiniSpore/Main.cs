using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MiniSpore
{
    public partial class Main : Form
    {
        public Main()
        {

            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            cbCommunicateMode.SelectedIndex = 0;

        }
        private void pbMin_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void pbClose_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
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

        }
        /// <summary>
        /// 应用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnApply_Click(object sender, EventArgs e)
        {

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
