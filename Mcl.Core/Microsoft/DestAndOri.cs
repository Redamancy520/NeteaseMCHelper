using System.Reflection;

namespace Microsoft
{
	internal class DestAndOri
	{
		public IMethodHook Obj;

		public MethodBase HookMethod { get; set; }

		public MethodBase OriginalMethod { get; set; }
	}
}
