using System;
using System.Reflection;

namespace Microsoft
{
	[AttributeUsage(AttributeTargets.Method)]
	public class HookMethodAttribute : Attribute
	{
		private string TargetMethodName;

		private string OriginalMethodName;

		public string TargetTypeFullName { get; private set; }

		public Type TargetType { get; private set; }

		public string GetTargetMethodName(MethodBase method)
		{
			if (!string.IsNullOrEmpty(TargetMethodName))
			{
				return TargetMethodName;
			}
			return method.Name;
		}

		public string GetOriginalMethodName(MethodBase method)
		{
			if (!string.IsNullOrEmpty(OriginalMethodName))
			{
				return OriginalMethodName;
			}
			return GetTargetMethodName(method) + "_Original";
		}

		public HookMethodAttribute(string targetTypeFullName, string targetMethodName = null, string originalMethodName = null)
		{
			TargetTypeFullName = targetTypeFullName;
			TargetMethodName = targetMethodName;
			OriginalMethodName = originalMethodName;
		}

		public HookMethodAttribute(Type targetType, string targetMethodName = null, string originalMethodName = null)
		{
			TargetType = targetType;
			TargetMethodName = targetMethodName;
			OriginalMethodName = originalMethodName;
		}
	}
}
