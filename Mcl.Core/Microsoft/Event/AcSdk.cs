using Eva;

using Util;
using WPFLauncher.Code;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Model;
using WPFLauncher.Network.Protocol;
using WPFLauncher.Util;

namespace Microsoft.Event
{
	internal class AcSdk : IMethodHook
	{


	
		//hook r方法 绕过指定目录的清空
		[HookMethod("WPFLauncher.Util.rx", null, "r_Original")]
		public static bool r(string etw)
		{
			if (etw == Tool.getGamePath() + @"\Game\.minecraft\resourcepacks")
			{
				StaticClient.send("rx.r", "Delete resourcepacks");
				return true;
			}
			if (etw == Tool.getGamePath() + @"\Game\.minecraft\shaderpacks")
			{
				StaticClient.send("rx.r", "Delete shaderpacks");
				return true;
			}
			return r_Original(etw);
		}
		
		[OriginalMethod]
		public static bool r_Original(string etw)
		{
			return true;

		}
		
		[HookMethod("WPFLauncher.Manager.akq", null, null)]
		private static void o()
		{
			StaticClient.send("AcSdkInit");
		}

		//CG播放, true:播放,false:不播放
		[HookMethod("WPFLauncher.Manager.Configuration.apq", null, null)]
		private static bool get_PlayCG()
		{

			if (StartMain.Config.IniReadValue("Setting", "PlayCG") == "true")
			{
				StaticClient.send("PlayCG", "true");
				return true;

			}
			else {
				StaticClient.send("PlayCG","false");
				return false;
			}
		}


	}
}
