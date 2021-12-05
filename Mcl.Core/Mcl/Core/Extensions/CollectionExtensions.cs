using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Mcl.Core.Extensions
{
	public static class CollectionExtensions
	{
		public static IEnumerable<T> AsEnumerable<T>(this T item)
		{
			return new T[1] { item };
		}

		public static IEnumerable<T> And<T>(this T item, T other)
		{
			return new T[2] { item, other };
		}

		public static IEnumerable<T> And<T>(this IEnumerable<T> items, T item)
		{
			foreach (T item2 in items)
			{
				yield return item2;
			}
			yield return item;
		}

		public static TK TryWithKey<T, TK>(this IDictionary<T, TK> dictionary, T key)
		{
			return dictionary.ContainsKey(key) ? dictionary[key] : default(TK);
		}

		public static IEnumerable<T> ToEnumerable<T>(this object[] items) where T : class
		{
			return items.Select((object item) => item as T);
		}

		public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
		{
			foreach (T item in items)
			{
				action(item);
			}
		}

		public static void AddRange(this IDictionary<string, string> collection, NameValueCollection range)
		{
			string[] allKeys = range.AllKeys;
			foreach (string text in allKeys)
			{
				collection.Add(text, range[text]);
			}
		}

		public static string ToQueryString(this NameValueCollection collection)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (collection.Count > 0)
			{
				stringBuilder.Append("?");
			}
			int num = 0;
			string[] allKeys = collection.AllKeys;
			foreach (string text in allKeys)
			{
				stringBuilder.AppendFormat("{0}={1}", text, collection[text].UrlEncode());
				num++;
				if (num < collection.Count)
				{
					stringBuilder.Append("&");
				}
			}
			return stringBuilder.ToString();
		}
	}
}
