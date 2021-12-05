using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions;

namespace Microsoft
{
	public class MethodHook
	{
		public static BindingFlags AllFlag = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		private static bool installed = false;

		private static List<DestAndOri> destAndOris = new List<DestAndOri>();


		public static void Install(string dir = null)
		{
			if (installed)
			{
				return;
			}
			installed = true;
			Assembly[] array = AppDomain.CurrentDomain.GetAssemblies();
			IEnumerable<IMethodHook> source;
			if (string.IsNullOrEmpty(dir))
			{
				source = array.SelectMany((Assembly t) => t.GetImplementedObjectsByInterface<IMethodHook>());
			}
			else
			{
				array = array.Concat(from x in Directory.GetFiles(dir, "*.dll").Select(delegate(string d)
					{
						try
						{
							return Assembly.LoadFrom(d);
						}
						catch
						{
							return null;
						}
					})
					where x != null
					select x).Distinct().ToArray();
				array = array.Concat(from x in Directory.GetFiles(dir, "*.exe").Select(delegate(string d)
					{
						try
						{
							return Assembly.LoadFrom(d);
						}
						catch
						{
							return null;
						}
					})
					where x != null
					select x).Distinct().ToArray();
				source = array.SelectMany((Assembly d) => d.GetImplementedObjectsByInterface<IMethodHook>());
			}
			source = source.Where((IMethodHook item) => item != null)?.ToList();
			foreach (IMethodHook item in source)
			{
				if (item == null)
				{
					continue;
				}
				MethodInfo[] methods = item.GetType().GetMethods(AllFlag);
				MethodInfo[] array2 = methods.Where((MethodInfo t) => t.GetCustomAttributesData().Any((CustomAttributeData a) => typeof(HookMethodAttribute).IsAssignableFrom(a.Constructor.DeclaringType))).ToArray();
				MethodInfo[] array3 = methods.Where((MethodInfo t) => t.GetCustomAttributesData().Any((CustomAttributeData a) => typeof(OriginalMethodAttribute).IsAssignableFrom(a.Constructor.DeclaringType))).ToArray();
				int num = array2.Count();
				MethodInfo[] array4 = array2;
				foreach (MethodInfo methodInfo in array4)
				{
					DestAndOri destAndOri = new DestAndOri();
					destAndOri.Obj = item;
					destAndOri.HookMethod = methodInfo;
					if (num == 1)
					{
						destAndOri.OriginalMethod = array3.FirstOrDefault();
					}
					else
					{
						string originalMethodName = TypeExtensions.GetCustomAttribute<HookMethodAttribute>(methodInfo).GetOriginalMethodName(methodInfo);
						MethodBase[] methods2 = array3;
						destAndOri.OriginalMethod = FindMethod(methods2, originalMethodName, methodInfo, array);
					}
					destAndOris.Add(destAndOri);
				}
			}
			InstallInternal(isInstall: true, array);
			AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
		}

		private static void InstallInternal(bool isInstall, Assembly[] assemblies)
		{
			foreach (DestAndOri destAndOri in destAndOris)
			{
				MethodBase hookMethod = destAndOri.HookMethod;
				HookMethodAttribute customAttribute = TypeExtensions.GetCustomAttribute<HookMethodAttribute>(hookMethod);
				string typeName = customAttribute.TargetTypeFullName;
				if (customAttribute.TargetType != null)
				{
					typeName = customAttribute.TargetType.FullName;
				}
				Type type = TypeResolver(typeName, assemblies);
				if (type != null && !assemblies.Contains(type.Assembly))
				{
					type = null;
				}
				string text = customAttribute.GetTargetMethodName(hookMethod);
				MethodBase methodBase = null;
				if (type != null)
				{
					MethodBase[] methods;
					if (text == type.Name || text == ".ctor")
					{
						MethodBase[] constructors = type.GetConstructors(AllFlag);
						methods = constructors;
						text = ".ctor";
					}
					else
					{
						MethodBase[] constructors = type.GetMethods(AllFlag);
						methods = constructors;
					}
					methodBase = FindMethod(methods, text, hookMethod, assemblies);
				}
				if (methodBase != null && methodBase.IsGenericMethod)
				{
					methodBase = ((MethodInfo)methodBase).MakeGenericMethod(hookMethod.GetParameters().Select(delegate(ParameterInfo o)
					{
						Type result = o.ParameterType;
						RememberTypeAttribute customAttribute2 = TypeExtensions.GetCustomAttribute<RememberTypeAttribute>(o);
						if (customAttribute2 != null && customAttribute2.TypeFullNameOrNull != null)
						{
							result = TypeResolver(customAttribute2.TypeFullNameOrNull, assemblies);
						}
						return result;
					}).ToArray());
				}
				if (methodBase == null)
				{
					if (!isInstall)
					{
					}
					continue;
				}
				if (destAndOri.Obj is IMethodHookWithSet)
				{
					((IMethodHookWithSet)destAndOri.Obj).HookMethod(methodBase);
				}
				MethodBase originalMethod = destAndOri.OriginalMethod;
				DetourFactory.CreateDetourEngine().Patch(methodBase, hookMethod, originalMethod);
			}
		}

		private static Type TypeResolver(string typeName, Assembly[] assemblies)
		{
			return Type.GetType(typeName, null, delegate(Assembly a, string b, bool c)
			{
				Type type;
				if (a != null)
				{
					type = a.GetType(b);
					if (type != null)
					{
						return type;
					}
				}
				type = Type.GetType(b);
				if (type != null)
				{
					return type;
				}
				Assembly[] array = assemblies;
				for (int i = 0; i < array.Length; i++)
				{
					type = array[i].GetType(b);
					if (type != null)
					{
						return type;
					}
				}
				return null;
			});
		}

		private static MethodBase FindMethod(MethodBase[] methods, string name, MethodBase like, Assembly[] assemblies)
		{
			ParameterInfo[] parameters = like.GetParameters();
			foreach (MethodBase methodBase in methods)
			{
				if (methodBase.Name != name)
				{
					continue;
				}
				ParameterInfo[] parameters2 = methodBase.GetParameters();
				int num = parameters2.Count();
				if (num != parameters.Count())
				{
					continue;
				}
				int num2 = 0;
				while (true)
				{
					if (num2 < num)
					{
						ParameterInfo parameterInfo = parameters[num2];
						ParameterInfo parameterInfo2 = parameters2[num2];
						if (!(parameterInfo.ParameterType.FullName == parameterInfo2.ParameterType.FullName))
						{
							RememberTypeAttribute customAttribute = TypeExtensions.GetCustomAttribute<RememberTypeAttribute>(parameterInfo);
							if (customAttribute == null || ((!customAttribute.IsGeneric || parameterInfo2.ParameterType.FullName != null) && (customAttribute.TypeFullNameOrNull == null || (!(customAttribute.TypeFullNameOrNull == parameterInfo2.ParameterType.FullName) && !(TypeResolver(customAttribute.TypeFullNameOrNull, assemblies) == parameterInfo2.ParameterType)))))
							{
								break;
							}
						}
						num2++;
						continue;
					}
					return methodBase;
				}
			}
			return null;
		}

		private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
		{
			InstallInternal(isInstall: false, new Assembly[1] { args.LoadedAssembly });
		}
	}
}
