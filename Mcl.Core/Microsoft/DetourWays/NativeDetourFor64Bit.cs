using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.DetourWays
{
	public class NativeDetourFor64Bit : NativeDetourFor32Bit
	{
		private byte[] jmp_inst = new byte[20]
		{
			80, 72, 184, 144, 144, 144, 144, 144, 144, 144,
			144, 80, 72, 139, 68, 36, 8, 194, 8, 0
		};

		protected unsafe override void MakePlacholderMethodCallPointsToRawMethod(MethodBase method)
		{
			uint num = LDasm.SizeofMin5Byte(rawMethodPtr);
			byte[] array = new byte[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = rawMethodPtr[i];
			}
			fixed (byte* ptr = &jmp_inst[3])
			{
				*(long*)ptr = (long)(rawMethodPtr + num);
			}
			int num2 = array.Length + jmp_inst.Length;
			IntPtr intPtr = Marshal.AllocHGlobal(num2);
			Marshal.Copy(array, 0, intPtr, array.Length);
			Marshal.Copy(jmp_inst, 0, intPtr + array.Length, jmp_inst.Length);
			NativeAPI.VirtualProtect(intPtr, (uint)num2, Protection.PAGE_EXECUTE_READWRITE, out var _);
			RuntimeHelpers.PrepareMethod(method.MethodHandle);
			*(long*)((byte*)method.MethodHandle.Value.ToPointer() + (nint)2 * (nint)4) = (long)intPtr;
		}
	}
}
