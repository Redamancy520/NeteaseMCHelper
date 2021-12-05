using System.Reflection;

namespace Microsoft
{
	public interface IDetour
	{
		void Patch(MethodBase rawMethod, MethodBase hookMethod, MethodBase originalMethod);
	}
}
