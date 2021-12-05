using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Util;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {

        static INIClass cfg = new INIClass(Program.getGamePath() + "\\Config.ini");


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cfg.IniWriteValue("Setting", "PlayCG", "true");
            cfg.IniWriteValue("Setting", "Chat", "false");
        }

        private void checkBox_PlayCG_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("请把需要添加的视频命名成 cg.mp4 放在该文件夹里","CG");
            System.Diagnostics.Process.Start("explorer.exe", Program.getWPFPath() + "\\Resource\\video");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("请把需要添加的光影放在这个文件夹里", "shaderpacks");
            System.Diagnostics.Process.Start("explorer.exe", Program.getGamePath() + "\\Game\\.minecraft\\shaderpacks");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("请把需要添加的材质包放在这个文件夹里", "Resource");
            System.Diagnostics.Process.Start("explorer.exe", Program.getGamePath() + "\\Game\\.minecraft\\resourcepacks");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                File.Delete(Program.getWPFPath() + "\\Mcl.Core.dll");
                File.Copy(Application.StartupPath +  "\\mcl.core.dll", Program.getWPFPath() + "\\Mcl.Core.dll");
                MessageBox.Show("初始化成功");
                Process.Start(Program.getWPFPath()+"\\WPFLauncher.exe");
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "初始化失败Error");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (checkBox_PlayCG.Checked)
            {
                cfg.IniWriteValue("Setting", "PlayCG", "true");
            }
            else
            {
                cfg.IniWriteValue("Setting", "PlayCG", "false");
            }


            if (checkBox1.Checked)
            {
                cfg.IniWriteValue("Chat", "msg", " [" + textBox_msg.Text + "]");
                cfg.IniWriteValue("Setting", "Chat", "true");
            }
            else {
                cfg.IniWriteValue("Setting","Chat","false");
            }
            
            MessageBox.Show("保存成功!\r\n将在下次网易我的世界盒子启动时加载","Setting");
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked) {
                textBox_msg.Enabled = true;
            }
            else
            {
                textBox_msg.Enabled = false;
            }
        }
    }
}
