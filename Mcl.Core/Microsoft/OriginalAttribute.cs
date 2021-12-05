using System;

namespace Microsoft
{
	[Obsolete("此类已变更为OriginalMethodAttribute")]
	[AttributeUsage(AttributeTargets.Method)]
	public class OriginalAttribute : OriginalMethodAttribute
	{
	}
}
