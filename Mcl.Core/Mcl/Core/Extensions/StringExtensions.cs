using System;
using System.Text;

namespace Mcl.Core.Extensions
{
	public static class StringExtensions
	{
		public static string UrlEncode(this string value)
		{
			return Uri.EscapeDataString(value);
		}

		public static string UrlDecode(this string value)
		{
			return Uri.UnescapeDataString(value);
		}

		public static string AsString(this byte[] buffer)
		{
			if (buffer == null)
			{
				return "";
			}
			Encoding uTF = Encoding.UTF8;
			return uTF.GetString(buffer, 0, buffer.Length);
		}

		public static bool HasValue(this string input)
		{
			return !string.IsNullOrEmpty(input);
		}
	}
}
