using System.Reflection;

namespace Microsoft
{
	public interface IMethodHookWithSet : IMethodHook
	{
		void HookMethod(MethodBase method);
	}
}
