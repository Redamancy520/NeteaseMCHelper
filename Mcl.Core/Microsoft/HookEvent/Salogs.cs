namespace Microsoft.HookEvent
{
	internal class Salogs : IMethodHook
	{

		/* @Change
		 * ax->az
		 */

		[HookMethod("WPFLauncher.az", null, null)]
		public static void h(string sn, string so)
		{
			StaticClient.send("h | " + sn, so);
		}

		[OriginalMethod]
		public static void h_Original(string sn, string so)
		{
		}

		[HookMethod("WPFLauncher.az", null, null)]
		public static void j(string sn, string so)
		{
			StaticClient.send("j | " + sn, so);
		}

		[OriginalMethod]
		public static void j_Original(string sn, string so)
		{
		}
	}
}
