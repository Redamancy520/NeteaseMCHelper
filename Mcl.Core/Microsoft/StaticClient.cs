
using System;
using System.Windows.Forms;
using Util;

namespace Microsoft
{
	internal class StaticClient
	{

		protected static Log log = new Log("hook");
		static StaticClient()
		{
			log.Write("hook inited.");
		}

        public static void send(string a, string b)
		{
			log.Write("|" + a + "|" + b);
		}

		public static void send(string a)
		{
			log.Write(a);
		}
	}
}
