namespace Microsoft.Event
{
	public class BaseInfo : IMethodHook
	{

		[HookMethod("WPFLauncher.Manager.Log.Util.amt", null, null)]
		public static string b()
		{
			string text = "";
			try
			{
				if (text == null || text.Length != 12)
				{
					text = Tool.getMac(b_Original());
				}


				return text;
			}
			catch
			{
				return text;
			}
		}

		[OriginalMethod]
		public static string b_Original()
		{
			return null;
		}

		[HookMethod("WPFLauncher.Manager.aka", null, null)]
		public static string f()
		{
			string text = "";
			try
			{
				if (text == null || text.Length != 8)
				{
					text = Tool.getDiskCode();
				}
				return text;
			}
			catch
			{
				return f_Original();
			}
		}

		[OriginalMethod]
		public static string f_Original()
		{
			return null;
		}

		[HookMethod("WPFLauncher.Manager.aka", null, null)]
		public static string d(string jmh)
		{
			string text = "";
			try
			{
				if (text == null || text.Length != 16)
				{
					text = Tool.getCPUID();
				}
				text += jmh;
				if (text.Length > 24)
				{
					text.Substring(0, 24);
					return text;
				}
				StaticClient.send("CPUID",text);
				return text;
			}
			catch
			{
				return d_Original(jmh);
			}
		}

		[OriginalMethod]
		public static string d_Original(string jmh)
		{
			return null;
		}

		[HookMethod("WPFLauncher.Manager.aka", null, null)]
		public static string g()
		{
			string text = "";
			try
			{
				if (text == null || text.Length != 12)
				{
					return Tool.getLocalIP(g_Original());
				}
				StaticClient.send("IP",text);
				return text;
			}
			catch
			{
				return g_Original();
			}
		}

		[OriginalMethod]
		public static string g_Original()
		{
			return null;
		}
	}
}
