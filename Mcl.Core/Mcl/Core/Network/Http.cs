using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Mcl.Core.Extensions;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network
{
	public class Http : IHttp, IHttpFactory
	{
		private TimeOutState timeoutState;

		private const string LINE_BREAK = "\r\n";

		private const string FORM_BOUNDARY = "-----------------------------28947758029299";

		private readonly IDictionary<string, Action<HttpWebRequest, string>> restrictedHeaderActions;

		protected bool HasParameters => Parameters.Any();

		protected bool HasCookies => Cookies.Any();

		protected bool HasBody => RequestBodyBytes != null || !string.IsNullOrEmpty(RequestBody);

		protected bool HasFiles => Files.Any();

		public bool AlwaysMultipartFormData { get; set; }

		public string UserAgent { get; set; }

		public int Timeout { get; set; }

		public int ReadWriteTimeout { get; set; }

		public ICredentials Credentials { get; set; }

		public CookieContainer CookieContainer { get; set; }

		public Action<Stream> ResponseWriter { get; set; }

		public IList<HttpFile> Files { get; }

		public bool FollowRedirects { get; set; }

		public X509CertificateCollection ClientCertificates { get; set; }

		public int? MaxRedirects { get; set; }

		public bool UseDefaultCredentials { get; set; }

		public Encoding Encoding { get; set; } = Encoding.UTF8;


		public IList<HttpHeader> Headers { get; }

		public IList<HttpParameter> Parameters { get; }

		public IList<HttpCookie> Cookies { get; }

		public string RequestBody { get; set; }

		public string RequestContentType { get; set; }

		public byte[] RequestBodyBytes { get; set; }

		public Uri Url { get; set; }

		public bool PreAuthenticate { get; set; }

		public RequestCachePolicy CachePolicy { get; set; }

		public HttpWebRequest DeleteAsync(Action<HttpResponse> action)
		{
			return GetStyleMethodInternalAsync("DELETE", action);
		}

		public HttpWebRequest GetAsync(Action<HttpResponse> action)
		{
			return GetStyleMethodInternalAsync("GET", action);
		}

		public HttpWebRequest HeadAsync(Action<HttpResponse> action)
		{
			return GetStyleMethodInternalAsync("HEAD", action);
		}

		public HttpWebRequest PostAsync(Action<HttpResponse> action)
		{
			return PutPostInternalAsync("POST", action);
		}

		public HttpWebRequest PutAsync(Action<HttpResponse> action)
		{
			return PutPostInternalAsync("PUT", action);
		}

		public HttpWebRequest PatchAsync(Action<HttpResponse> action)
		{
			return PutPostInternalAsync("PATCH", action);
		}

		public HttpWebRequest AsPostAsync(Action<HttpResponse> action, string httpMethod)
		{
			return PutPostInternalAsync(httpMethod.ToUpperInvariant(), action);
		}

		public HttpWebRequest AsGetAsync(Action<HttpResponse> action, string httpMethod)
		{
			return GetStyleMethodInternalAsync(httpMethod.ToUpperInvariant(), action);
		}

		protected virtual HttpWebRequest ConfigureAsyncWebRequest(string method, Uri url)
		{
			return ConfigureWebRequest(method, url);
		}

		private void SetTimeout(IAsyncResult asyncResult, TimeOutState timeOutState)
		{
			if (Timeout != 0)
			{
				ThreadPool.RegisterWaitForSingleObject(asyncResult.AsyncWaitHandle, TimeoutCallback, timeOutState, Timeout, executeOnlyOnce: true);
			}
		}

		private static void TimeoutCallback(object state, bool timedOut)
		{
			if (!timedOut)
			{
				return;
			}
			TimeOutState timeOutState = state as TimeOutState;
			if (timeOutState != null)
			{
				lock (timeOutState)
				{
					timeOutState.TimedOut = true;
				}
				timeOutState.Request?.Abort();
			}
		}

		private static void GetRawResponseAsync(IAsyncResult result, Action<HttpWebResponse> callback)
		{
			HttpWebResponse httpWebResponse = null;
			try
			{
				HttpWebRequest httpWebRequest = (HttpWebRequest)result.AsyncState;
				httpWebResponse = httpWebRequest.EndGetResponse(result) as HttpWebResponse;
			}
			catch (WebException ex)
			{
				if (ex.Status == WebExceptionStatus.RequestCanceled)
				{
					throw;
				}
				if (!(ex.Response is HttpWebResponse))
				{
					throw;
				}
				httpWebResponse = ex.Response as HttpWebResponse;
			}
			callback(httpWebResponse);
			httpWebResponse?.Close();
		}

		private HttpResponse CreateErrorResponse(Exception ex)
		{
			HttpResponse httpResponse = new HttpResponse();
			WebException ex2 = ex as WebException;
			if (ex2 != null && ex2.Status == WebExceptionStatus.RequestCanceled)
			{
				httpResponse.ResponseStatus = (timeoutState.TimedOut ? ResponseStatus.TimedOut : ResponseStatus.Aborted);
				return httpResponse;
			}
			httpResponse.ErrorMessage = ex.Message;
			httpResponse.ErrorException = ex;
			httpResponse.ResponseStatus = ResponseStatus.Error;
			return httpResponse;
		}

		private void ResponseCallback(IAsyncResult result, Action<HttpResponse> callback)
		{
			HttpResponse response = new HttpResponse
			{
				ResponseStatus = ResponseStatus.None
			};
			try
			{
				if (timeoutState.TimedOut)
				{
					response.ResponseStatus = ResponseStatus.TimedOut;
					ExecuteCallback(response, callback);
					return;
				}
				GetRawResponseAsync(result, delegate(HttpWebResponse webResponse)
				{
					try
					{
						ExtractResponseData(response, webResponse);
						ExecuteCallback(response, callback);
					}
					catch
					{
					}
				});
			}
			catch (Exception ex)
			{
				ExecuteCallback(CreateErrorResponse(ex), callback);
			}
		}

		private static void ExecuteCallback(HttpResponse response, Action<HttpResponse> callback)
		{
			PopulateErrorForIncompleteResponse(response);
			callback(response);
		}

		private static void PopulateErrorForIncompleteResponse(HttpResponse response)
		{
			if (response.ResponseStatus != ResponseStatus.Completed && response.ErrorException == null)
			{
				response.ErrorException = response.ResponseStatus.ToWebException();
				response.ErrorMessage = response.ErrorException.Message;
			}
		}

		private long CalculateContentLength()
		{
			if (RequestBodyBytes != null)
			{
				return RequestBodyBytes.Length;
			}
			if (!HasFiles && !AlwaysMultipartFormData)
			{
				return Encoding.GetByteCount(RequestBody);
			}
			long num = 0L;
			foreach (HttpFile file in Files)
			{
				num += Encoding.GetByteCount(GetMultipartFileHeader(file));
				num += file.ContentLength;
				num += Encoding.GetByteCount("\r\n");
			}
			num = Parameters.Aggregate(num, (long current, HttpParameter param) => current + Encoding.GetByteCount(GetMultipartFormData(param)));
			return num + Encoding.GetByteCount(GetMultipartFooter());
		}

		private void RequestStreamCallback(IAsyncResult result, Action<HttpResponse> callback, HttpWebRequest httpWebRequest)
		{
			HttpWebRequest httpWebRequest2 = (HttpWebRequest)result.AsyncState;
			if (timeoutState.TimedOut)
			{
				HttpResponse response = new HttpResponse
				{
					ResponseStatus = ResponseStatus.TimedOut
				};
				ExecuteCallback(response, callback);
				return;
			}
			try
			{
				using Stream stream = httpWebRequest2.EndGetRequestStream(result);
				if (HasFiles || AlwaysMultipartFormData)
				{
					WriteMultipartFormData(stream);
				}
				else if (RequestBodyBytes != null)
				{
					stream.Write(RequestBodyBytes, 0, RequestBodyBytes.Length);
				}
				else if (RequestBody != null)
				{
					WriteStringTo(stream, RequestBody);
				}
			}
			catch (Exception ex)
			{
				HttpResponse httpResponse = CreateErrorResponse(ex);
				httpResponse.ProtocolVersion = httpWebRequest.ProtocolVersion;
				ExecuteCallback(httpResponse, callback);
				return;
			}
			IAsyncResult asyncResult = httpWebRequest2.BeginGetResponse(delegate(IAsyncResult r)
			{
				ResponseCallback(r, callback);
			}, httpWebRequest2);
			SetTimeout(asyncResult, timeoutState);
		}

		private void WriteRequestBodyAsync(HttpWebRequest webRequest, Action<HttpResponse> callback)
		{
			timeoutState = new TimeOutState
			{
				Request = webRequest
			};
			IAsyncResult asyncResult;
			if (HasBody || HasFiles || AlwaysMultipartFormData)
			{
				webRequest.ContentLength = CalculateContentLength();
				asyncResult = webRequest.BeginGetRequestStream(delegate(IAsyncResult result)
				{
					RequestStreamCallback(result, callback, webRequest);
				}, webRequest);
			}
			else
			{
				asyncResult = webRequest.BeginGetResponse(delegate(IAsyncResult r)
				{
					ResponseCallback(r, callback);
				}, webRequest);
			}
			SetTimeout(asyncResult, timeoutState);
		}

		private HttpWebRequest PutPostInternalAsync(string method, Action<HttpResponse> callback)
		{
			HttpWebRequest httpWebRequest = null;
			try
			{
				httpWebRequest = ConfigureAsyncWebRequest(method, Url);
				PreparePostBody(httpWebRequest);
				WriteRequestBodyAsync(httpWebRequest, callback);
			}
			catch (Exception ex)
			{
				ExecuteCallback(CreateErrorResponse(ex), callback);
			}
			return httpWebRequest;
		}

		private HttpWebRequest GetStyleMethodInternalAsync(string method, Action<HttpResponse> callback)
		{
			HttpWebRequest httpWebRequest = null;
			try
			{
				Uri url = Url;
				httpWebRequest = ConfigureAsyncWebRequest(method, url);
				if (HasBody && method == "DELETE")
				{
					httpWebRequest.ContentType = RequestContentType;
					WriteRequestBodyAsync(httpWebRequest, callback);
				}
				else
				{
					timeoutState = new TimeOutState
					{
						Request = httpWebRequest
					};
					IAsyncResult asyncResult = httpWebRequest.BeginGetResponse(delegate(IAsyncResult result)
					{
						ResponseCallback(result, callback);
					}, httpWebRequest);
					SetTimeout(asyncResult, timeoutState);
				}
			}
			catch (Exception ex)
			{
				ExecuteCallback(CreateErrorResponse(ex), callback);
			}
			return httpWebRequest;
		}

		public Http()
		{
			Headers = new List<HttpHeader>();
			Files = new List<HttpFile>();
			Parameters = new List<HttpParameter>();
			Cookies = new List<HttpCookie>();
			restrictedHeaderActions = new Dictionary<string, Action<HttpWebRequest, string>>(StringComparer.OrdinalIgnoreCase);
			AddSharedHeaderActions();
			AddSyncHeaderActions();
		}

		public IHttp Create()
		{
			return new Http();
		}

		protected virtual HttpWebRequest CreateWebRequest(Uri url)
		{
			return (HttpWebRequest)WebRequest.Create(url);
		}

		private void AddSyncHeaderActions()
		{
			restrictedHeaderActions.Add("Connection", delegate(HttpWebRequest r, string v)
			{
				r.KeepAlive = v.ToLower().Contains("keep-alive");
			});
			restrictedHeaderActions.Add("Content-Length", delegate(HttpWebRequest r, string v)
			{
				r.ContentLength = Convert.ToInt64(v);
			});
			restrictedHeaderActions.Add("Expect", delegate(HttpWebRequest r, string v)
			{
				r.Expect = v;
			});
			restrictedHeaderActions.Add("If-Modified-Since", delegate(HttpWebRequest r, string v)
			{
				r.IfModifiedSince = Convert.ToDateTime(v, CultureInfo.InvariantCulture);
			});
			restrictedHeaderActions.Add("Referer", delegate(HttpWebRequest r, string v)
			{
				r.Referer = v;
			});
			restrictedHeaderActions.Add("Transfer-Encoding", delegate(HttpWebRequest r, string v)
			{
				r.TransferEncoding = v;
				r.SendChunked = true;
			});
			restrictedHeaderActions.Add("User-Agent", delegate(HttpWebRequest r, string v)
			{
				r.UserAgent = v;
			});
		}

		private void AddSharedHeaderActions()
		{
			restrictedHeaderActions.Add("Accept", delegate(HttpWebRequest r, string v)
			{
				r.Accept = v;
			});
			restrictedHeaderActions.Add("Content-Type", delegate(HttpWebRequest r, string v)
			{
				r.ContentType = v;
			});
			restrictedHeaderActions.Add("Date", delegate(HttpWebRequest r, string v)
			{
				if (DateTime.TryParse(v, out var result))
				{
					r.Date = result;
				}
			});
			restrictedHeaderActions.Add("Host", delegate(HttpWebRequest r, string v)
			{
				r.Host = v;
			});
			restrictedHeaderActions.Add("Range", AddRange);
		}

		private static string GetMultipartFormContentType()
		{
			return string.Format("multipart/form-data; boundary={0}", "-----------------------------28947758029299");
		}

		private static string GetMultipartFileHeader(HttpFile file)
		{
			return string.Format("--{0}{4}Content-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"{4}Content-Type: {3}{4}{4}", "-----------------------------28947758029299", file.Name, file.FileName, file.ContentType ?? "application/octet-stream", "\r\n");
		}

		private string GetMultipartFormData(HttpParameter param)
		{
			string format = ((param.Name == RequestContentType) ? "--{0}{3}Content-Type: {4}{3}Content-Disposition: form-data; name=\"{1}\"{3}{3}{2}{3}" : "--{0}{3}Content-Disposition: form-data; name=\"{1}\"{3}{3}{2}{3}");
			return string.Format(format, "-----------------------------28947758029299", param.Name, param.Value, "\r\n", param.ContentType);
		}

		private static string GetMultipartFooter()
		{
			return string.Format("--{0}--{1}", "-----------------------------28947758029299", "\r\n");
		}

		private void AppendHeaders(HttpWebRequest webRequest)
		{
			foreach (HttpHeader header in Headers)
			{
				if (restrictedHeaderActions.ContainsKey(header.Name))
				{
					restrictedHeaderActions[header.Name](webRequest, header.Value);
				}
				else
				{
					webRequest.Headers.Add(header.Name, header.Value);
				}
			}
		}

		private void AppendCookies(HttpWebRequest webRequest)
		{
			webRequest.CookieContainer = CookieContainer ?? new CookieContainer();
			foreach (HttpCookie cookie2 in Cookies)
			{
				Cookie cookie = new Cookie
				{
					Name = cookie2.Name,
					Value = cookie2.Value,
					Domain = webRequest.RequestUri.Host
				};
				webRequest.CookieContainer.Add(cookie);
			}
		}

		private string EncodeParameters()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (HttpParameter parameter in Parameters)
			{
				if (stringBuilder.Length > 1)
				{
					stringBuilder.Append("&");
				}
				stringBuilder.AppendFormat("{0}={1}", parameter.Name.UrlEncode(), parameter.Value.UrlEncode());
			}
			return stringBuilder.ToString();
		}

		private void PreparePostBody(HttpWebRequest webRequest)
		{
			bool flag = string.IsNullOrEmpty(webRequest.ContentType);
			if (HasFiles || AlwaysMultipartFormData)
			{
				if (flag)
				{
					webRequest.ContentType = GetMultipartFormContentType();
				}
			}
			else if (HasParameters)
			{
				if (flag)
				{
					webRequest.ContentType = "application/x-www-form-urlencoded";
				}
				RequestBody = EncodeParameters();
			}
			else if (HasBody && flag)
			{
				webRequest.ContentType = RequestContentType;
			}
		}

		private void WriteStringTo(Stream stream, string toWrite)
		{
			byte[] bytes = Encoding.GetBytes(toWrite);
			stream.Write(bytes, 0, bytes.Length);
		}

		private void WriteMultipartFormData(Stream requestStream)
		{
			foreach (HttpParameter parameter in Parameters)
			{
				WriteStringTo(requestStream, GetMultipartFormData(parameter));
			}
			foreach (HttpFile file in Files)
			{
				WriteStringTo(requestStream, GetMultipartFileHeader(file));
				file.Writer(requestStream);
				WriteStringTo(requestStream, "\r\n");
			}
			WriteStringTo(requestStream, GetMultipartFooter());
		}

		private void ExtractResponseData(HttpResponse response, HttpWebResponse webResponse)
		{
			using (webResponse)
			{
				response.ContentEncoding = webResponse.ContentEncoding;
				response.Server = webResponse.Server;
				response.ContentType = webResponse.ContentType;
				response.ContentLength = webResponse.ContentLength;
				Stream responseStream = webResponse.GetResponseStream();
				ProcessResponseStream(responseStream, response);
				response.StatusCode = webResponse.StatusCode;
				response.StatusDescription = webResponse.StatusDescription;
				response.ResponseUri = webResponse.ResponseUri;
				response.ResponseStatus = ResponseStatus.Completed;
				if (webResponse.Cookies != null)
				{
					foreach (Cookie cookie in webResponse.Cookies)
					{
						response.Cookies.Add(new HttpCookie
						{
							Comment = cookie.Comment,
							CommentUri = cookie.CommentUri,
							Discard = cookie.Discard,
							Domain = cookie.Domain,
							Expired = cookie.Expired,
							Expires = cookie.Expires,
							HttpOnly = cookie.HttpOnly,
							Name = cookie.Name,
							Path = cookie.Path,
							Port = cookie.Port,
							Secure = cookie.Secure,
							TimeStamp = cookie.TimeStamp,
							Value = cookie.Value,
							Version = cookie.Version
						});
					}
				}
				string[] allKeys = webResponse.Headers.AllKeys;
				foreach (string name in allKeys)
				{
					string value = webResponse.Headers[name];
					response.Headers.Add(new HttpHeader
					{
						Name = name,
						Value = value
					});
				}
				webResponse.Close();
			}
		}

		private void ProcessResponseStream(Stream webResponseStream, HttpResponse response)
		{
			if (ResponseWriter == null)
			{
				response.RawBytes = webResponseStream.ReadAsBytes();
			}
			else
			{
				ResponseWriter(webResponseStream);
			}
		}

		private static void AddRange(HttpWebRequest r, string range)
		{
			Match match = Regex.Match(range, "(\\w+)=(\\d+)-(\\d+)$");
			if (match.Success)
			{
				string value = match.Groups[1].Value;
				long from = Convert.ToInt64(match.Groups[2].Value);
				long to = Convert.ToInt64(match.Groups[3].Value);
				r.AddRange(value, from, to);
			}
		}

		public HttpResponse Delete()
		{
			return GetStyleMethodInternal("DELETE");
		}

		public HttpResponse Get()
		{
			return GetStyleMethodInternal("GET");
		}

		public HttpResponse Head()
		{
			return GetStyleMethodInternal("HEAD");
		}

		public HttpResponse Post()
		{
			return PostPutInternal("POST");
		}

		public HttpResponse Put()
		{
			return PostPutInternal("PUT");
		}

		public HttpResponse Patch()
		{
			return PostPutInternal("PATCH");
		}

		public HttpResponse AsGet(string httpMethod)
		{
			return GetStyleMethodInternal(httpMethod.ToUpperInvariant());
		}

		public HttpResponse AsPost(string httpMethod)
		{
			return PostPutInternal(httpMethod.ToUpperInvariant());
		}

		private HttpResponse GetStyleMethodInternal(string method)
		{
			HttpWebRequest httpWebRequest = ConfigureWebRequest(method, Url);
			if (HasBody && method == "DELETE")
			{
				httpWebRequest.ContentType = RequestContentType;
				WriteRequestBody(httpWebRequest);
			}
			return GetResponse(httpWebRequest);
		}

		private HttpResponse PostPutInternal(string method)
		{
			HttpWebRequest httpWebRequest = ConfigureWebRequest(method, Url);
			PreparePostBody(httpWebRequest);
			WriteRequestBody(httpWebRequest);
			return GetResponse(httpWebRequest);
		}

		private static void ExtractErrorResponse(IHttpResponse httpResponse, Exception ex)
		{
			WebException ex2 = ex as WebException;
			if (ex2 != null && ex2.Status == WebExceptionStatus.Timeout)
			{
				httpResponse.ResponseStatus = ResponseStatus.TimedOut;
				httpResponse.ErrorMessage = ex.Message;
				httpResponse.ErrorException = ex2;
			}
			else
			{
				httpResponse.ErrorMessage = ex.Message;
				httpResponse.ErrorException = ex;
				httpResponse.ResponseStatus = ResponseStatus.Error;
			}
		}

		private HttpResponse GetResponse(HttpWebRequest request)
		{
			HttpResponse httpResponse = new HttpResponse
			{
				ResponseStatus = ResponseStatus.None
			};
			try
			{
				HttpWebResponse rawResponse = GetRawResponse(request);
				ExtractResponseData(httpResponse, rawResponse);
			}
			catch (Exception ex)
			{
				ExtractErrorResponse(httpResponse, ex);
			}
			return httpResponse;
		}

		private static HttpWebResponse GetRawResponse(HttpWebRequest request)
		{
			try
			{
				return (HttpWebResponse)request.GetResponse();
			}
			catch (WebException ex)
			{
				HttpWebResponse httpWebResponse = ex.Response as HttpWebResponse;
				if (httpWebResponse != null)
				{
					return httpWebResponse;
				}
				throw;
			}
		}

		private void WriteRequestBody(HttpWebRequest webRequest)
		{
			if (HasBody || HasFiles || AlwaysMultipartFormData)
			{
				webRequest.ContentLength = CalculateContentLength();
			}
			using Stream stream = webRequest.GetRequestStream();
			if (HasFiles || AlwaysMultipartFormData)
			{
				WriteMultipartFormData(stream);
			}
			else if (RequestBodyBytes != null)
			{
				stream.Write(RequestBodyBytes, 0, RequestBodyBytes.Length);
			}
			else if (RequestBody != null)
			{
				WriteStringTo(stream, RequestBody);
			}
		}

		protected virtual HttpWebRequest ConfigureWebRequest(string method, Uri url)
		{
			HttpWebRequest httpWebRequest = CreateWebRequest(url);
			httpWebRequest.Proxy = null;
			httpWebRequest.UseDefaultCredentials = UseDefaultCredentials;
			httpWebRequest.PreAuthenticate = PreAuthenticate;
			AppendHeaders(httpWebRequest);
			AppendCookies(httpWebRequest);
			httpWebRequest.Method = method;
			if (!HasFiles && !AlwaysMultipartFormData)
			{
				httpWebRequest.ContentLength = 0L;
			}
			if (Credentials != null)
			{
				httpWebRequest.Credentials = Credentials;
			}
			if (UserAgent.HasValue())
			{
				httpWebRequest.UserAgent = UserAgent;
			}
			if (ClientCertificates != null)
			{
				httpWebRequest.ClientCertificates.AddRange(ClientCertificates);
			}
			httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			if (Timeout != 0)
			{
				httpWebRequest.Timeout = Timeout;
			}
			if (ReadWriteTimeout != 0)
			{
				httpWebRequest.ReadWriteTimeout = ReadWriteTimeout;
			}
			if (CachePolicy != null)
			{
				httpWebRequest.CachePolicy = CachePolicy;
			}
			httpWebRequest.AllowAutoRedirect = FollowRedirects;
			if (FollowRedirects && MaxRedirects.HasValue)
			{
				httpWebRequest.MaximumAutomaticRedirections = MaxRedirects.Value;
			}
			return httpWebRequest;
		}
	}
}
