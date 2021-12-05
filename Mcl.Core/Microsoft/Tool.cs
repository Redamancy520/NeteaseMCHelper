using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Newtonsoft.Json;
using WPFLauncher.Manager.LanGame;

namespace Microsoft
{
	public class Tool
	{
		private static string HKEY_BASE = "SOFTWARE\\Netease\\MCLauncher";

		private static string mainPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

		public static string randomStr(int len, string[] arr = null)
		{
			if (arr == null || arr.Length <= 1)
			{
				arr = new string[16]
				{
					"a", "b", "c", "d", "e", "f", "0", "1", "2", "3",
					"4", "5", "6", "7", "8", "9"
				};
			}
			string text = "";
			for (int i = 0; i < len; i++)
			{
				text += arr[new Random(new Random(Guid.NewGuid().GetHashCode()).Next(0, 100)).Next(arr.Length - 1)];
			}
			return text;
		}

		public static string randomStr_Unicode(int len)
		{
			string[] arr = new string[73]
			{
				"\ud83d\ude04", "\ud83c\udd92", "\ud83d\ude2d", "\ud83d\udc37", "\ud83c\udf5f", "\ud83d\udc4d", "\ud83d\udeb2", "❌", "\ud83e\uddd4", "\ud83e\udd11",
				"\ud83d\ude1c", "\ud83d\ude0b", "\ud83d\ude21", "\ud83d\ude00", "\ud83d\ude18", "\ud83d\udc4c", "\ud83d\ude04", "\ud83d\ude24", "\ud83d\udc32", "\ud83d\udc7b",
				"\ud83d\udc41", "\ud83d\udd2e", "⚔", "⚽", "\ud83d\udea6", "\ud83c\udf69", "\ud83c\udf59", "\ud83e\udd53", "\ud83c\udf56", "\ud83d\udc71\u200d",
				"\ud83c\udfa0", "₯", "\ud835\udd6c", "\ud835\udd9c", "\ud835\udd8e", "\ud835\udd98", "\ud835\udd8d", "\ud835\udd8b", "\ud835\udd94", "\ud835\udd92",
				"\ud835\udd9e", "\ud835\udd95", "\ud835\udd8e", "\ud835\udd93", "\ud835\udd88", "\ud835\udd8a", "\ud835\udd98", "\ud835\udd6c", "\ud835\udd6d", "\ud835\udd6e",
				"\ud835\udd6f", "\ud835\udd70", "\ud835\udd71", "\ud835\udd72", "\ud835\udd73", "\ud835\udd74", "\ud835\udd75", "\ud835\udd76", "\ud835\udd77", "\ud835\udd78",
				"\ud835\udd79", "\ud835\udd7a", "\ud835\udd7b", "\ud835\udd7c", "\ud835\udd7d", "\ud835\udd7e", "\ud835\udd7f", "\ud835\udd80", "\ud835\udd81", "\ud835\udd82",
				"\ud835\udd83", "\ud835\udd84", "\ud835\udd85"
			};
			return randomStr(len, arr);
		}

		public static string randomMac(string source = null)
		{
			string text = "";
			int num = 12;
			if (source != null)
			{
				num = source.Length;
			}
			for (int i = 1; i <= num; i++)
			{
				text = ((i % 2 != 0) ? (text + randomStr(1)) : (i switch
				{
					2 => text + randomStr(1, new string[8] { "0", "2", "4", "6", "8", "A", "C", "E" }), 
					12 => text + randomStr(1, new string[15]
					{
						"0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
						"A", "B", "C", "D", "E"
					}), 
					_ => text + randomStr(1), 
				}));
			}
			return text.ToUpper();
		}





		public static string getMainPath()
		{
			if (mainPath != null && File.Exists(mainPath))
			{
				return mainPath;
			}
			return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
		}

		public static string getMac(string source)
		{
			string text = null;
			if (text == null || text.Length != 12)
			{
				text = randomMac(source).ToUpper();
				if (text.Length > 12)
				{
					text = text.Substring(0, 12);
				}
			}
			return text;
		}

		public static string getLocalIP(string source)
		{
			string text = null;
			if (text == null || text.Length < 7)
			{
				text = randomIP(source).ToUpper();
				if (text.Length > 15)
				{
					text = text.Substring(0, 15);
				}
			}
			return text;
		}

		public static string randomIP(string source)
		{
			Random random = new Random();
			string text = "";
			for (int i = 0; i <= 3; i++)
			{
				string text2 = random.Next(0, 255).ToString();
				text = ((i >= 3) ? (text + text2.ToString()) : (text + (text2 + ".").ToString()));
			}
			if (Regex.IsMatch(text, "^((25[0-5]|2[0-4]\\d|((1\\d{2})|([1-9]?\\d)))\\.){3}(25[0-5]|2[0-4]\\d|((1\\d{2})|([1-9]?\\d)))$"))
			{
				return text;
			}
			return "";
		}

		public static string getDiskCode()
		{
			string text = null;
			if (text == null || text.Length != 8)
			{
				text = randomStr(8).ToUpper();
			}
			return text;
		}

		public static string getCPUID()
		{
			string text = null;
			if (text == null || text.Length != 16)
			{
				text = randomStr(16).ToUpper();
			}
			return text;
		}

		public static string getGamePath()
		{
			try
			{
				return (string)Registry.CurrentUser.OpenSubKey(HKEY_BASE).GetValue("DownloadPath");
			}
			catch (Exception)
			{
				return "C:\\MCLDownload";
			}
		}

		public static string runCMD(string cmdline, bool waiteForExit = true)
		{
			string result = "";
			Process process = new Process();
			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.FileName = "cmd.exe";
			processStartInfo.Arguments = "/c " + cmdline;
			processStartInfo.UseShellExecute = false;
			processStartInfo.RedirectStandardInput = false;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.CreateNoWindow = true;
			process.StartInfo = processStartInfo;
			try
			{
				process.Start();
				if (waiteForExit)
				{
					process.WaitForExit();
				}
				result = process.StandardOutput.ReadToEnd();
				return result;
			}
			catch
			{
				return result;
			}
			finally
			{
				process.Kill();
				process.Close();
			}
		}

		public static string toString()
		{
			return "";
		}

		public static string getProcessCmd(int pid)
		{
			string result = "";
			string cmdline = "wmic process where ProcessId=" + pid + " get commandline";
			try
			{
				result = runCMD(cmdline);
				return result;
			}
			catch (Exception ex)
			{
				StaticClient.send(ex.ToString());
				return result;
			}
		}



		public static bool copyFile(string source, string target)
		{
			if (!File.Exists(source))
			{
				return false;
			}
			try
			{
				if (deleteFile(target))
				{
					File.Copy(source, target);
				}
			}
			catch
			{
			}
			return false;
		}

		public static bool deleteFile(string source)
		{
			if (!File.Exists(source))
			{
				return true;
			}
			try
			{
				File.Delete(source);
			}
			catch
			{
			}
			return File.Exists(source);
		}



		public static byte[] objectsToBytes(params object[] ktb)
		{
			if (ktb == null)
			{
				return null;
			}
			byte[] array = new byte[0];
			foreach (object obj in ktb)
			{
				byte[] array2 = new byte[0];
				Type type = obj.GetType();
				switch (Type.GetTypeCode(type))
				{
				case TypeCode.Object:
					if (type == typeof(byte[]))
					{
						array2 = (byte[])obj;
					}
					else if (type == typeof(List<uint>))
					{
						List<byte> list = new List<byte>();
						byte[] bytes = BitConverter.GetBytes((ushort)(((List<uint>)obj).Count * 4));
						list.AddRange(array2);
						list.AddRange(bytes);
						foreach (uint item in obj as List<uint>)
						{
							list.AddRange(BitConverter.GetBytes(item));
						}
						array2 = list.ToArray();
					}
					else if (type == typeof(List<ulong>))
					{
						List<byte> list2 = new List<byte>();
						byte[] bytes2 = BitConverter.GetBytes((ushort)(((List<ulong>)obj).Count * 8));
						list2.AddRange(array2);
						list2.AddRange(bytes2);
						foreach (ulong item2 in obj as List<ulong>)
						{
							list2.AddRange(BitConverter.GetBytes(item2));
						}
						array2 = list2.ToArray();
					}
					else if (type == typeof(List<long>))
					{
						List<byte> list3 = new List<byte>();
						byte[] bytes3 = BitConverter.GetBytes((ushort)(((List<long>)obj).Count * 8));
						list3.AddRange(array2);
						list3.AddRange(bytes3);
						foreach (long item3 in obj as List<long>)
						{
							list3.AddRange(BitConverter.GetBytes(item3));
						}
						array2 = list3.ToArray();
					}
					else if (type == typeof(GameDescription))
					{
						string text = JsonConvert.SerializeObject(obj);
						array2 = objectsToBytes(text);
					}
					break;
				case TypeCode.Boolean:
					array2 = BitConverter.GetBytes((bool)obj);
					break;
				case TypeCode.Byte:
					array2 = new byte[1] { (byte)obj };
					break;
				case TypeCode.Int16:
					array2 = BitConverter.GetBytes((short)obj);
					break;
				case TypeCode.UInt16:
					array2 = BitConverter.GetBytes((ushort)obj);
					break;
				case TypeCode.Int32:
					array2 = BitConverter.GetBytes((int)obj);
					break;
				case TypeCode.UInt32:
					array2 = BitConverter.GetBytes((uint)obj);
					break;
				case TypeCode.Int64:
					array2 = BitConverter.GetBytes((long)obj);
					break;
				case TypeCode.Double:
					array2 = BitConverter.GetBytes((double)obj);
					break;
				case TypeCode.String:
					array2 = Encoding.UTF8.GetBytes((string)obj);
					array2 = objectsToBytes((ushort)array2.Length, array2);
					break;
				}
				array = array.Concat(array2).ToArray();
			}
			return array;
		}

	}
}
