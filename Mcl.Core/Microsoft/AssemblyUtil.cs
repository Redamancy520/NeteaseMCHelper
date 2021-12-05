using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Microsoft
{
	public static class AssemblyUtil
	{
		private static object[] m_EmptyObjectArray = new object[0];

		public static T CreateInstance<T>(string type)
		{
			return CreateInstance<T>(type, new object[0]);
		}

		public static T CreateInstance<T>(string type, object[] parameters)
		{
			Type type2 = null;
			type2 = Type.GetType(type, throwOnError: false, ignoreCase: true);
			if (type2 == null)
			{
				return default(T);
			}
			return (T)Activator.CreateInstance(type2, parameters);
		}

		public static T CreateInstance<T>(string assembleName, string type)
		{
			Type type2 = null;
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				if (!string.Equals(assembly.FullName, assembleName, StringComparison.CurrentCultureIgnoreCase))
				{
					continue;
				}
				Type[] types = assembly.GetTypes();
				foreach (Type type3 in types)
				{
					if (string.Equals(type3.ToString(), type, StringComparison.CurrentCultureIgnoreCase))
					{
						type2 = type3;
						break;
					}
				}
				break;
			}
			if (type2 == null)
			{
				return default(T);
			}
			return (T)Activator.CreateInstance(type2, new object[0]);
		}

		public static T CreateInstance<T>(string assembleName, string type, object[] parameters)
		{
			Type type2 = null;
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				if (!string.Equals(assembly.FullName, assembleName, StringComparison.CurrentCultureIgnoreCase))
				{
					continue;
				}
				Type[] types = assembly.GetTypes();
				foreach (Type type3 in types)
				{
					if (string.Equals(type3.ToString(), type, StringComparison.CurrentCultureIgnoreCase))
					{
						type2 = type3;
						break;
					}
				}
				break;
			}
			if (type2 == null)
			{
				return default(T);
			}
			return (T)Activator.CreateInstance(type2, parameters);
		}

		public static IEnumerable<Type> GetImplementTypes<TBaseType>(this Assembly assembly)
		{
			return from t in assembly.GetExportedTypes()
				where t.IsSubclassOf(typeof(TBaseType)) && t.IsClass && !t.IsAbstract
				select t;
		}

		public static IEnumerable<TBaseInterface> GetImplementedObjectsByInterface<TBaseInterface>(this Assembly assembly) where TBaseInterface : class
		{
			return assembly.GetImplementedObjectsByInterface<TBaseInterface>(typeof(TBaseInterface));
		}

		public static IEnumerable<TBaseInterface> GetImplementedObjectsByInterface<TBaseInterface>(this Assembly assembly, Type targetType) where TBaseInterface : class
		{
			Type[] types = assembly.GetTypes();
			List<TBaseInterface> list = new List<TBaseInterface>();
			foreach (Type type in types)
			{
				if (!type.IsAbstract && targetType.IsAssignableFrom(type))
				{
					list.Add((TBaseInterface)Activator.CreateInstance(type));
				}
			}
			return list;
		}

		public static T BinaryClone<T>(this T target)
		{
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			using MemoryStream memoryStream = new MemoryStream();
			binaryFormatter.Serialize(memoryStream, target);
			memoryStream.Position = 0L;
			return (T)binaryFormatter.Deserialize(memoryStream);
		}

		public static T CopyPropertiesTo<T>(this T source, T target)
		{
			Dictionary<string, PropertyInfo> dictionary = source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty).ToDictionary((PropertyInfo p) => p.Name);
			PropertyInfo[] properties = target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
			foreach (PropertyInfo propertyInfo in properties)
			{
				if (dictionary.TryGetValue(propertyInfo.Name, out var value) && !(value.PropertyType != propertyInfo.PropertyType) && value.PropertyType.IsSerializable)
				{
					propertyInfo.SetValue(target, value.GetValue(source, m_EmptyObjectArray), m_EmptyObjectArray);
				}
			}
			return target;
		}

		public static IEnumerable<Assembly> GetAssembliesFromString(string assemblyDef)
		{
			return GetAssembliesFromStrings(assemblyDef.Split(new char[2] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));
		}

		public static IEnumerable<Assembly> GetAssembliesFromStrings(string[] assemblies)
		{
			List<Assembly> list = new List<Assembly>(assemblies.Length);
			foreach (string assemblyString in assemblies)
			{
				list.Add(Assembly.Load(assemblyString));
			}
			return list;
		}

		public static string GetAssembleVer(string filePath)
		{
			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(filePath);
			return $" {versionInfo.ProductMajorPart}.{versionInfo.ProductMinorPart}.{versionInfo.ProductBuildPart}.{versionInfo.ProductPrivatePart}";
		}
	}
}
