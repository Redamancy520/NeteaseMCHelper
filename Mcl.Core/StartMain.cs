using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Mcl.Core.Utils;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Auth;
using WPFLauncher.Manager.Chat;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Manager.Game;
using WPFLauncher.Manager.LanGame;
using WPFLauncher.Manager.Log;
using WPFLauncher.Manager.Login;
using WPFLauncher.Update;
using WPFLauncher.Util;
using WPFLauncher.Util.UI;
using WPFLauncher.View;
using WPFLauncher.View.UI;
using Microsoft;
using System.Windows;
using WPFLauncher.Model;
using Microsoft.Event;
using Util;

namespace Eva
{
	public class StartMain
	{
		public static bool inited = false;

		public static INIClass Config = new INIClass(Tool.getGamePath() + "\\Config.ini");



		public static string GetWebClient(string url)
		{
			string strHTML = "";
			WebClient myWebClient = new WebClient();
			Stream myStream = myWebClient.OpenRead(url);
			StreamReader sr = new StreamReader(myStream, System.Text.Encoding.GetEncoding("utf-8"));
			strHTML = sr.ReadToEnd();
			myStream.Close();
			return strHTML;
		}

		public static void start(string msg = null)
		{

			if (inited == true)
			{
				return;
			}
			try
			{
				inited = true;

				qt.n("Hook Load");
				Control.CheckForIllegalCrossThreadCalls = true;
				System.Windows.Forms.Application.EnableVisualStyles();

				MethodHook.Install();
				return;
			}
			catch (Exception ex)
			{
				StaticClient.send("inited exception:", ex.ToString());
				killMe(-1);
			}


		}

        public static void killMe(int pid = -1)
		{
		if (pid <= 0)
		{
			pid = Process.GetCurrentProcess().Id;
		}
		Process process = new Process();
		process.StartInfo.FileName = "cmd.exe";
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.RedirectStandardInput = true;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.CreateNoWindow = true;
		process.Start();
		string text = "taskkill /PID " + pid;
		process.StandardInput.WriteLine(text + "&exit");
		process.StandardInput.AutoFlush = true;
		process.StandardOutput.ReadToEnd();
		process.WaitForExit();
		process.Close();
		Process.GetCurrentProcess().Kill();
			}
		}
	}
 

