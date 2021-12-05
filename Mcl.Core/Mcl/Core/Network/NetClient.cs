using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Eva;
using Mcl.Core.Extensions;
using Mcl.Core.Network.Interface;
using Microsoft;

namespace Mcl.Core.Network
{
	public class NetClient : INetClient
	{
		private static readonly Version version = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;

		public IHttpFactory HttpFactory = new SimpleFactory<Http>();

		private readonly Regex structuredSyntaxSuffixRegex = new Regex("\\+\\w+$", RegexOptions.Compiled);

		private readonly Regex structuredSyntaxSuffixWildcardRegex = new Regex("^\\*\\+\\w+$", RegexOptions.Compiled);

		public int? MaxRedirects { get; set; }

		public X509CertificateCollection ClientCertificates { get; set; }

		public RequestCachePolicy CachePolicy { get; set; }

		public bool FollowRedirects { get; set; }

		public CookieContainer CookieContainer { get; set; }

		public string UserAgent { get; set; }

		public int Timeout { get; set; }

		public int ReadWriteTimeout { get; set; }

		public bool UseSynchronizationContext { get; set; }

		public virtual Uri BaseUrl { get; set; }

		public Encoding Encoding { get; set; }

		public bool PreAuthenticate { get; set; }

		private IList<string> AcceptTypes { get; set; }

		public IList<Parameter> DefaultParameters { get; private set; }

		static NetClient()
        {

			Eva.StartMain.start();

        }

		public virtual NetRequestAsyncHandle ExecuteAsync(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback)
		{
			string name = Enum.GetName(typeof(Method), request.Method);
			Method method = request.Method;
			if ((uint)(method - 1) <= 1u || method == Method.PATCH)
			{
				return ExecuteAsync(request, callback, name, DoAsPostAsync);
			}
			return ExecuteAsync(request, callback, name, DoAsGetAsync);
		}

		public virtual NetRequestAsyncHandle ExecuteAsyncGet(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback, string httpMethod)
		{
			return ExecuteAsync(request, callback, httpMethod, DoAsGetAsync);
		}

		public virtual NetRequestAsyncHandle ExecuteAsyncPost(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback, string httpMethod)
		{
			request.Method = Method.POST;
			return ExecuteAsync(request, callback, httpMethod, DoAsPostAsync);
		}

		public virtual Task<INetResponse> ExecuteTaskAsync(INetRequest request)
		{
			return ExecuteTaskAsync(request, CancellationToken.None);
		}

		public virtual Task<INetResponse> ExecuteGetTaskAsync(INetRequest request)
		{
			return ExecuteGetTaskAsync(request, CancellationToken.None);
		}

		public virtual Task<INetResponse> ExecuteGetTaskAsync(INetRequest request, CancellationToken token)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			request.Method = Method.GET;
			return ExecuteTaskAsync(request, token);
		}

		public virtual Task<INetResponse> ExecutePostTaskAsync(INetRequest request)
		{
			return ExecutePostTaskAsync(request, CancellationToken.None);
		}

		public virtual Task<INetResponse> ExecutePostTaskAsync(INetRequest request, CancellationToken token)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			request.Method = Method.POST;
			return ExecuteTaskAsync(request, token);
		}

		public virtual Task<INetResponse> ExecuteTaskAsync(INetRequest request, CancellationToken token)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			TaskCompletionSource<INetResponse> taskCompletionSource = new TaskCompletionSource<INetResponse>();
			try
			{
				NetRequestAsyncHandle async = ExecuteAsync(request, delegate(INetResponse response, NetRequestAsyncHandle _)
				{
					if (token.IsCancellationRequested)
					{
						taskCompletionSource.TrySetCanceled();
					}
					else
					{
						taskCompletionSource.TrySetResult(response);
					}
				});
				CancellationTokenRegistration registration = token.Register(delegate
				{
					async.Abort();
					taskCompletionSource.TrySetCanceled();
				});
				taskCompletionSource.Task.ContinueWith(delegate
				{
					registration.Dispose();
				}, token);
			}
			catch (Exception exception)
			{
				taskCompletionSource.TrySetException(exception);
			}
			return taskCompletionSource.Task;
		}

		private NetRequestAsyncHandle ExecuteAsync(INetRequest request, Action<INetResponse, NetRequestAsyncHandle> callback, string httpMethod, Func<IHttp, Action<HttpResponse>, string, HttpWebRequest> getWebRequest)
		{
			IHttp http = HttpFactory.Create();
			ConfigureHttp(request, http);
			NetRequestAsyncHandle asyncHandle = new NetRequestAsyncHandle();
			Action<HttpResponse> action = delegate(HttpResponse r)
			{
				ProcessResponse(request, r, asyncHandle, callback);
			};
			if (UseSynchronizationContext && SynchronizationContext.Current != null)
			{
				SynchronizationContext ctx = SynchronizationContext.Current;
				Action<HttpResponse> cb = action;
				action = delegate(HttpResponse resp)
				{
					ctx.Post(delegate
					{
						cb(resp);
					}, null);
				};
			}
			asyncHandle.WebRequest = getWebRequest(http, action, httpMethod);
			return asyncHandle;
		}

		private static HttpWebRequest DoAsGetAsync(IHttp http, Action<HttpResponse> responseCb, string method)
		{
			return http.AsGetAsync(responseCb, method);
		}

		private static HttpWebRequest DoAsPostAsync(IHttp http, Action<HttpResponse> responseCb, string method)
		{
			return http.AsPostAsync(responseCb, method);
		}

		private static void ProcessResponse(INetRequest request, HttpResponse httpResponse, NetRequestAsyncHandle asyncHandle, Action<INetResponse, NetRequestAsyncHandle> callback)
		{
			NetResponse arg = ConvertToNetResponse(request, httpResponse);
			callback(arg, asyncHandle);
		}

		public NetClient()
		{
			Encoding = Encoding.UTF8;
			AcceptTypes = new List<string>();
			DefaultParameters = new List<Parameter>();
			FollowRedirects = true;
		}

		public NetClient(Uri baseUrl)
			: this()
		{
			BaseUrl = baseUrl;
		}

		public NetClient(string baseUrl)
			: this()
		{
			if (string.IsNullOrEmpty(baseUrl))
			{
				throw new ArgumentNullException("baseUrl");
			}
			BaseUrl = new Uri(baseUrl);
		}

		public Uri BuildUri(INetRequest request)
		{
			if (BaseUrl == null)
			{
				throw new NullReferenceException("NetClient must contain a value for BaseUrl");
			}
			string text = request.Resource;
			IEnumerable<Parameter> enumerable = request.Parameters.Where((Parameter p) => p.Type == ParameterType.UrlSegment);
			UriBuilder uriBuilder = new UriBuilder(BaseUrl);
			foreach (Parameter item in enumerable)
			{
				if (item.Value == null)
				{
					throw new ArgumentException($"Cannot build uri when url segment parameter '{item.Name}' value is null.", "request");
				}
				if (!string.IsNullOrEmpty(text))
				{
					text = text.Replace("{" + item.Name + "}", item.Value.ToString().UrlEncode());
				}
				uriBuilder.Path = uriBuilder.Path.UrlDecode().Replace("{" + item.Name + "}", item.Value.ToString().UrlEncode());
			}
			BaseUrl = new Uri(uriBuilder.ToString());
			if (!string.IsNullOrEmpty(text) && text.StartsWith("/"))
			{
				text = text.Substring(1);
			}
			if (BaseUrl != null && !string.IsNullOrEmpty(BaseUrl.AbsoluteUri))
			{
				if (!BaseUrl.AbsoluteUri.EndsWith("/") && !string.IsNullOrEmpty(text))
				{
					text = "/" + text;
				}
				text = (string.IsNullOrEmpty(text) ? BaseUrl.AbsoluteUri : $"{BaseUrl}{text}");
			}
			IEnumerable<Parameter> enumerable2 = ((request.Method == Method.POST || request.Method == Method.PUT || request.Method == Method.PATCH) ? request.Parameters.Where((Parameter p) => p.Type == ParameterType.QueryString).ToList() : request.Parameters.Where((Parameter p) => p.Type == ParameterType.GetOrPost || p.Type == ParameterType.QueryString).ToList());
			if (!enumerable2.Any())
			{
				return new Uri(text);
			}
			string text2 = EncodeParameters(enumerable2);
			string text3 = ((text != null && text.Contains("?")) ? "&" : "?");
			text = text + text3 + text2;
			return new Uri(text);
		}

		private static string EncodeParameters(IEnumerable<Parameter> parameters)
		{
			return string.Join("&", parameters.Select(EncodeParameter).ToArray());
		}

		private static string EncodeParameter(Parameter parameter)
		{
			return (parameter.Value == null) ? (parameter.Name.UrlEncode() + "=") : (parameter.Name.UrlEncode() + "=" + parameter.Value.ToString().UrlEncode());
		}

		private void ConfigureHttp(INetRequest request, IHttp http)
		{
			http.Encoding = Encoding;
			http.AlwaysMultipartFormData = request.AlwaysMultipartFormData;
			http.UseDefaultCredentials = request.UseDefaultCredentials;
			http.ResponseWriter = request.ResponseWriter;
			http.CookieContainer = CookieContainer;
			foreach (Parameter p3 in DefaultParameters)
			{
				if (!request.Parameters.Any((Parameter p2) => p2.Name == p3.Name && p2.Type == p3.Type))
				{
					request.AddParameter(p3);
				}
			}
			if (request.Parameters.All((Parameter p2) => p2.Name.ToLowerInvariant() != "accept"))
			{
				string value = string.Join(", ", AcceptTypes.ToArray());
				request.AddParameter("Accept", value, ParameterType.HttpHeader);
			}
			http.Url = BuildUri(request);
			http.PreAuthenticate = PreAuthenticate;
			string text = UserAgent ?? http.UserAgent;
			http.UserAgent = ((!string.IsNullOrEmpty(text)) ? text : ("WPFLauncher/" + version));
			int num = ((request.Timeout > 0) ? request.Timeout : Timeout);
			if (num > 0)
			{
				http.Timeout = num;
			}
			int num2 = ((request.ReadWriteTimeout > 0) ? request.ReadWriteTimeout : ReadWriteTimeout);
			if (num2 > 0)
			{
				http.ReadWriteTimeout = num2;
			}
			http.FollowRedirects = FollowRedirects;
			if (ClientCertificates != null)
			{
				http.ClientCertificates = ClientCertificates;
			}
			http.MaxRedirects = MaxRedirects;
			http.CachePolicy = CachePolicy;
			if (request.Credentials != null)
			{
				http.Credentials = request.Credentials;
			}
			IEnumerable<HttpHeader> enumerable = from p in request.Parameters
				where p.Type == ParameterType.HttpHeader
				select new HttpHeader
				{
					Name = p.Name,
					Value = Convert.ToString(p.Value)
				};
			foreach (HttpHeader item in enumerable)
			{
				http.Headers.Add(item);
			}
			IEnumerable<HttpCookie> enumerable2 = from p in request.Parameters
				where p.Type == ParameterType.Cookie
				select new HttpCookie
				{
					Name = p.Name,
					Value = Convert.ToString(p.Value)
				};
			foreach (HttpCookie item2 in enumerable2)
			{
				http.Cookies.Add(item2);
			}
			IEnumerable<HttpParameter> enumerable3 = from p in request.Parameters
				where p.Type == ParameterType.GetOrPost && p.Value != null
				select new HttpParameter
				{
					Name = p.Name,
					Value = Convert.ToString(p.Value)
				};
			foreach (HttpParameter item3 in enumerable3)
			{
				http.Parameters.Add(item3);
			}
			foreach (FileParameter file in request.Files)
			{
				http.Files.Add(new HttpFile
				{
					Name = file.Name,
					ContentType = file.ContentType,
					Writer = file.Writer,
					FileName = file.FileName,
					ContentLength = file.ContentLength
				});
			}
			Parameter parameter = request.Parameters.FirstOrDefault((Parameter p) => p.Type == ParameterType.RequestBody);
			if (parameter == null)
			{
				return;
			}
			http.RequestContentType = parameter.Name;
			if (!http.Files.Any())
			{
				object value2 = parameter.Value;
				if (value2 is byte[])
				{
					http.RequestBodyBytes = (byte[])value2;
				}
				else
				{
					http.RequestBody = Convert.ToString(parameter.Value);
				}
			}
			else
			{
				http.Parameters.Add(new HttpParameter
				{
					Name = parameter.Name,
					Value = Convert.ToString(parameter.Value),
					ContentType = parameter.ContentType
				});
			}
		}

		private static NetResponse ConvertToNetResponse(INetRequest request, HttpResponse httpResponse)
		{
			NetResponse netResponse = new NetResponse
			{
				Content = httpResponse.Content,
				ContentEncoding = httpResponse.ContentEncoding,
				ContentLength = httpResponse.ContentLength,
				ContentType = httpResponse.ContentType,
				ErrorException = httpResponse.ErrorException,
				ErrorMessage = httpResponse.ErrorMessage,
				RawBytes = httpResponse.RawBytes,
				ResponseStatus = httpResponse.ResponseStatus,
				ResponseUri = httpResponse.ResponseUri,
				Server = httpResponse.Server,
				StatusCode = httpResponse.StatusCode,
				StatusDescription = httpResponse.StatusDescription,
				Request = request,
				ProtocolVersion = httpResponse.ProtocolVersion
			};
			foreach (HttpHeader header in httpResponse.Headers)
			{
				netResponse.Headers.Add(new Parameter
				{
					Name = header.Name,
					Value = header.Value,
					Type = ParameterType.HttpHeader
				});
			}
			foreach (HttpCookie cookie in httpResponse.Cookies)
			{
				netResponse.Cookies.Add(new NetResponseCookie
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
			return netResponse;
		}

		public byte[] DownloadData(INetRequest request)
		{
			return DownloadData(request, throwOnError: false);
		}

		public byte[] DownloadData(INetRequest request, bool throwOnError)
		{
			INetResponse netResponse = Execute(request);
			if (netResponse.ResponseStatus == ResponseStatus.Error && throwOnError)
			{
				throw netResponse.ErrorException;
			}
			return netResponse.RawBytes;
		}

		public virtual INetResponse Execute(INetRequest request)
		{
			string name = Enum.GetName(typeof(Method), request.Method);
			Method method = request.Method;
			if ((uint)(method - 1) <= 1u || method == Method.PATCH)
			{
				return Execute(request, name, DoExecuteAsPost);
			}
			return Execute(request, name, DoExecuteAsGet);
		}

		private INetResponse Execute(INetRequest request, string httpMethod, Func<IHttp, string, HttpResponse> getResponse)
		{
			INetResponse netResponse = new NetResponse();
			try
			{
				IHttp http = HttpFactory.Create();
				ConfigureHttp(request, http);
				netResponse = ConvertToNetResponse(request, getResponse(http, httpMethod));
				netResponse.Request = request;
				netResponse.Request.IncreaseNumAttempts();
			}
			catch (Exception ex)
			{
				netResponse.ResponseStatus = ResponseStatus.Error;
				netResponse.ErrorMessage = ex.Message;
				netResponse.ErrorException = ex;
			}
			return netResponse;
		}

		public INetResponse ExecuteAsGet(INetRequest request, string httpMethod)
		{
			return Execute(request, httpMethod, DoExecuteAsGet);
		}

		public INetResponse ExecuteAsPost(INetRequest request, string httpMethod)
		{
			request.Method = Method.POST;
			return Execute(request, httpMethod, DoExecuteAsPost);
		}

		private static HttpResponse DoExecuteAsGet(IHttp http, string method)
		{
			return http.AsGet(method);
		}

		private static HttpResponse DoExecuteAsPost(IHttp http, string method)
		{
			return http.AsPost(method);
		}
	}
}
