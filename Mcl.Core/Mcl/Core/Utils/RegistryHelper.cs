#define DEBUG
using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace Mcl.Core.Utils
{
	public class RegistryHelper
	{
		private static RegistryKey registrykey = null;

		public static void InitRegistryKey(string path)
		{
			try
			{
				registrykey = Registry.CurrentUser.CreateSubKey(path);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.StackTrace);
			}
		}

		public static void SetValue(string key, string value)
		{
			try
			{
				registrykey?.SetValue(key, value);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.StackTrace);
			}
		}

		public static string GetValue(string key)
		{
			try
			{
				return registrykey?.GetValue(key)?.ToString();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.StackTrace);
				return null;
			}
		}
	}
}
