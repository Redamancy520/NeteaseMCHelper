using System;

namespace Microsoft
{
	[Obsolete("此类已变更为HookMethodAttribute")]
	[AttributeUsage(AttributeTargets.Method)]
	public class MonitorAttribute : HookMethodAttribute
	{
		public MonitorAttribute(string NamespaceName, string ClassName)
			: base(NamespaceName + "." + ClassName)
		{
		}

		public MonitorAttribute(Type type)
			: base(type)
		{
		}
	}
}
