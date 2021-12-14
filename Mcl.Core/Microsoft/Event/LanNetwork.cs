using Eva;
using System;
using System.Threading;

namespace Microsoft.Event
{
	internal class LanNetwork : IMethodHook
	{

   

		private static ushort[] blokArray = new ushort[1] { 19 };

        
        [HookMethod("WPFLauncher.Manager.LanGame.alz", "a", null)]
        public static byte[] lan(params object[] ktb)
        {

            int num = 0;
            bool flag = false;
            bool flag2 = false;
            string str = "{";
            int i = 0;
            while (i < ktb.Length)
            {
                object obj = ktb[i];
                try
                {
                    if (obj != null)
                    {
                        if (num == 0 && obj.ToString().Equals("18"))
                        {
                            flag = true;
                        }
                        if (num == 0 && obj.ToString().Equals("768"))
                        {
                            flag2 = true;
                        }
                        str = str + "'" + obj.ToString() + "',";
                        if (num == 2 && flag)
                        {
                            string text = obj.ToString();
                            if (!text.StartsWith("\\") && !text.StartsWith("/") && !text.StartsWith("-") && !text.StartsWith("."))
                            {
                                Random random = new Random();

                                if (StartMain.Config.IniReadValue("Setting", "Chat") == "true")
                                {
                                    ktb[num] = text + StartMain.Config.IniReadValue("Chat", "msg");
                                }
                                else
                                {
                                    ktb[num] = text;
                                }
                                
                            }
                        }
                        if (flag2)
                        {
                            ktb[num] = "";
                        }
                        goto IL_DB;
                    }
                }
                catch
                {
                    goto IL_DB;
                }
            IL_D3:
                i++;
                continue;
            IL_DB:
                num++;
                goto IL_D3;
            }
            str += "}";
            StaticClient.send("LanGame.alv", str);
            byte[] result = null;
            try
            {
                result = Tool.objectsToBytes(ktb);
            }
            catch
            {
                result = new byte[0];
            }
            return result;
        }

        
        [HookMethod("WPFLauncher.Network.xj", null, null)]
		public void a(ushort fkk, Action<byte[]> fkl)
		{
			try
			{
				a_Original(fkk, fkl);
				StaticClient.send("Network.xj a :" + fkk, fkl.Method.ToString() + " " + fkl.Target.ToString());
			}
			catch (Exception)
			{
			}
		}

		[OriginalMethod]
		public void a_Original(ushort fkk, Action<byte[]> fkl)
		{
		}

		[HookMethod("WPFLauncher.Network.xj", null, null)]
		public void b(ushort fkk, byte[] fkl)
		{
			try
			{
				if (fkk == 19)
				{
					StaticClient.send("client_pcyc_check");
					return;
				}
				ushort[] array = blokArray;
				foreach (int num in array)
				{
					if (num == fkk)
					{
						switch (num)
						{
						case 17:
						case 18:
						case 19:
						case 261:
							return;
						}
						_ = 273;
						return;
					}
				}
				b_Original(fkk, fkl);
			}
			catch (Exception)
			{
			}
		}

		[OriginalMethod]
		public void b_Original(ushort fkk, byte[] fkl)
		{
		}
	}
}
