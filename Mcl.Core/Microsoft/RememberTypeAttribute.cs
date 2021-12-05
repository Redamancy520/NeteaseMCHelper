using System;

namespace Microsoft
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class RememberTypeAttribute : Attribute
	{
		public string TypeFullNameOrNull { get; private set; }

		public bool IsGeneric { get; private set; }

		public RememberTypeAttribute(string fullName = null, bool isGeneric = false)
		{
			TypeFullNameOrNull = fullName;
			IsGeneric = isGeneric;
		}
	}
}
