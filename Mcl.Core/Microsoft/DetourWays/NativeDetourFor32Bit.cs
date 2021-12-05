using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.DetourWays
{
	public class NativeDetourFor32Bit : IDetour
	{
		protected byte[] newInstrs = new byte[5] { 233, 144, 144, 144, 144 };

		protected unsafe byte* rawMethodPtr;

		public unsafe virtual void Patch(MethodBase rawMethod, MethodBase hookMethod, MethodBase originalMethod)
		{
			RuntimeTypeHandle[] instantiation = (from t in rawMethod.DeclaringType.GetGenericArguments()
				select t.TypeHandle).ToArray();
			RuntimeHelpers.PrepareMethod(rawMethod.MethodHandle, instantiation);
			rawMethodPtr = (byte*)rawMethod.MethodHandle.GetFunctionPointer().ToPointer();
			byte* ptr = (byte*)hookMethod.MethodHandle.GetFunctionPointer().ToPointer();
			fixed (byte* ptr2 = newInstrs)
			{
				*(int*)(ptr2 + 1) = (int)ptr - (int)rawMethodPtr - 5;
			}
			if (originalMethod != null)
			{
				MakePlacholderMethodCallPointsToRawMethod(originalMethod);
			}
			Patch();
		}

		protected unsafe virtual void Patch()
		{
			NativeAPI.VirtualProtect((IntPtr)rawMethodPtr, 5u, Protection.PAGE_EXECUTE_READWRITE, out var _);
			for (int i = 0; i < newInstrs.Length; i++)
			{
				rawMethodPtr[i] = newInstrs[i];
			}
		}

		protected unsafe virtual void MakePlacholderMethodCallPointsToRawMethod(MethodBase originalMethod)
		{
			uint num = LDasm.SizeofMin5Byte(rawMethodPtr);
			int num2 = (int)(num + 5);
			byte[] array = new byte[num2];
			IntPtr intPtr = Marshal.AllocHGlobal(num2);
			for (int i = 0; i < num; i++)
			{
				array[i] = rawMethodPtr[i];
			}
			array[num] = 233;
			fixed (byte* ptr = &array[num + 1])
			{
				*(int*)ptr = (int)rawMethodPtr - (int)intPtr - 5;
			}
			Marshal.Copy(array, 0, intPtr, num2);
			NativeAPI.VirtualProtect(intPtr, (uint)num2, Protection.PAGE_EXECUTE_READWRITE, out var _);
			RuntimeHelpers.PrepareMethod(originalMethod.MethodHandle);
			*(int*)((byte*)originalMethod.MethodHandle.Value.ToPointer() + (nint)2 * (nint)4) = (int)intPtr;
		}
	}
}
