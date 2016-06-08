#region References

using System;
using System.Data.Entity.Infrastructure;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http.Filters;

#endregion

namespace Scribe.Website.Attributes
{
	public class WebApiExceptionFilterAttribute : ExceptionFilterAttribute
	{
		#region Methods

		public override void OnException(HttpActionExecutedContext context)
		{
			var debugging = HttpContext.Current.IsDebuggingEnabled;

			if (context.Exception is NotImplementedException)
			{
				context.Response = context.Request.CreateResponse(HttpStatusCode.NotImplemented);
				return;
			}

			if (context.Exception is DbUpdateException)
			{
				var message = GetMessage(context.Exception.ToDetailedString());
				context.Response = context.Request.CreateErrorResponse(HttpStatusCode.BadRequest, message);
				return;
			}

			if (context.Exception is ArgumentException)
			{
				var message = context.Exception.CleanMessage();

				context.Response = debugging
					? context.Request.CreateErrorResponse(HttpStatusCode.BadRequest, message, context.Exception)
					: context.Request.CreateErrorResponse(HttpStatusCode.BadRequest, message);

				return;
			}

			context.Response = debugging
				? context.Request.CreateErrorResponse(HttpStatusCode.BadRequest, context.Exception.Message, context.Exception)
				: context.Request.CreateErrorResponse(HttpStatusCode.BadRequest, context.Exception.Message);
		}

		private string GetMessage(string message)
		{
			if (message.Contains("IX_Pages_Title"))
			{
				return "The page title is already been used.";
			}

			return message;
		}

		#endregion
	}
}