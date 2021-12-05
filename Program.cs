using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp1
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }


        private static string HKEY_BASE = "SOFTWARE\\Netease\\MCLauncher";
        public static string getGamePath()
        {
            try
            {
                return (string)Registry.CurrentUser.OpenSubKey(HKEY_BASE).GetValue("DownloadPath");
            }
            catch (Exception)
            {
                return "C:\\MCLDownload";
            }
        }
        public static string getWPFPath() {
            try
            {
                return (string)Registry.CurrentUser.OpenSubKey(HKEY_BASE).GetValue("InstallLocation");
            }
            catch (Exception)
            {
                return "C:\\Program Files (x86)\\Netease\\MCLauncher";
            }
        }

    }
}
