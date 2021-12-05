using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions
{
	public static class TypeExtensions
	{
		public static T GetCustomAttribute<T>(this MemberInfo @this)
		{
			return (T)(@this.GetCustomAttributes(typeof(T), inherit: true)?.ToList()).FirstOrDefault();
		}

		public static T GetCustomAttribute<T>(this ParameterInfo @this)
		{
			return (T)(@this.GetCustomAttributes(typeof(T), inherit: true)?.ToList()).FirstOrDefault();
		}
	}
}
