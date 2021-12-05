using System;
using System.Collections.Generic;
using System.Net;
using Mcl.Core.Extensions;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network
{
	public abstract class NetResponseBase
	{
		private string content;

		public INetRequest Request { get; set; }

		public string ContentType { get; set; }

		public long ContentLength { get; set; }

		public string ContentEncoding { get; set; }

		public string Content
		{
			get
			{
				return content ?? (content = RawBytes.AsString());
			}
			set
			{
				content = value;
			}
		}

		public HttpStatusCode StatusCode { get; set; }

		public bool IsSuccessful => StatusCode >= HttpStatusCode.OK && StatusCode <= (HttpStatusCode)299 && ResponseStatus == ResponseStatus.Completed;

		public string StatusDescription { get; set; }

		public byte[] RawBytes { get; set; }

		public Uri ResponseUri { get; set; }

		public string Server { get; set; }

		public IList<NetResponseCookie> Cookies { get; protected internal set; }

		public IList<Parameter> Headers { get; protected internal set; }

		public ResponseStatus ResponseStatus { get; set; }

		public string ErrorMessage { get; set; }

		public Exception ErrorException { get; set; }

		public Version ProtocolVersion { get; set; }

		protected NetResponseBase()
		{
			ResponseStatus = ResponseStatus.None;
			Headers = new List<Parameter>();
			Cookies = new List<NetResponseCookie>();
		}

		protected string DebuggerDisplay()
		{
			return $"StatusCode: {StatusCode}, Content-Type: {ContentType}, Content-Length: {ContentLength})";
		}
	}
}
