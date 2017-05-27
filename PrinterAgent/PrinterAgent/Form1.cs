using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Diagnostics;

namespace PrinterAgent
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            notifyIcon1.BalloonTipTitle = "打印助手加载完成";
            notifyIcon1.BalloonTipText = "监听 " + ConfigurationManager.AppSettings["HttpEndpoint"];
            notifyIcon1.BalloonTipText += "\r\n请右键点击打印助手任务栏图标，进入开燃云集系统！";
            notifyIcon1.ShowBalloonTip(30);
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void 进入开燃云集系统ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(ConfigurationManager.AppSettings["ChromePath"], ConfigurationManager.AppSettings["ChromeParameter"]);
        }
    }
}
