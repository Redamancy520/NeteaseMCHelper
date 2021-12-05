using System.IO;
using Mcl.Core.Network;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Extensions
{
	public static class MiscExtensions
	{
		public static byte[] ReadAsBytes(this Stream input)
		{
			byte[] array = new byte[16384];
			using MemoryStream memoryStream = new MemoryStream();
			int count;
			while ((count = input.Read(array, 0, array.Length)) > 0)
			{
				memoryStream.Write(array, 0, count);
			}
			return memoryStream.ToArray();
		}

		public static INetResponse<T> ToAsyncResponse<T>(this INetResponse response)
		{
			return new NetResponse<T>
			{
				ContentEncoding = response.ContentEncoding,
				ContentLength = response.ContentLength,
				ContentType = response.ContentType,
				Cookies = response.Cookies,
				ErrorException = response.ErrorException,
				ErrorMessage = response.ErrorMessage,
				Headers = response.Headers,
				RawBytes = response.RawBytes,
				ResponseStatus = response.ResponseStatus,
				ResponseUri = response.ResponseUri,
				Server = response.Server,
				StatusCode = response.StatusCode,
				StatusDescription = response.StatusDescription
			};
		}
	}
}
