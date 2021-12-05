using System;
using System.Net;
using Mcl.Core.Network;

namespace Mcl.Core.Extensions
{
	public static class ResponseStatusExtensions
	{
		public static WebException ToWebException(this ResponseStatus responseStatus)
		{
			return responseStatus switch
			{
				ResponseStatus.None => new WebException("The request could not be processed.", WebExceptionStatus.ServerProtocolViolation), 
				ResponseStatus.Error => new WebException("An error occurred while processing the request.", WebExceptionStatus.ServerProtocolViolation), 
				ResponseStatus.TimedOut => new WebException("The request timed-out.", WebExceptionStatus.Timeout), 
				ResponseStatus.Aborted => new WebException("The request was aborted.", WebExceptionStatus.Timeout), 
				_ => throw new ArgumentOutOfRangeException("responseStatus"), 
			};
		}
	}
}
