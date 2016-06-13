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

		public override void OnException(HttpActionExecutedContext database)
		{
			var debugging = HttpContext.Current.IsDebuggingEnabled;

			if (database.Exception is NotImplementedException)
			{
				database.Response = database.Request.CreateResponse(HttpStatusCode.NotImplemented);
				return;
			}

			if (database.Exception is DbUpdateException)
			{
				var message = GetMessage(database.Exception.ToDetailedString());
				database.Response = database.Request.CreateErrorResponse(HttpStatusCode.BadRequest, message);
				return;
			}

			if (database.Exception is ArgumentException)
			{
				var message = database.Exception.CleanMessage();

				database.Response = debugging
					? database.Request.CreateErrorResponse(HttpStatusCode.BadRequest, message, database.Exception)
					: database.Request.CreateErrorResponse(HttpStatusCode.BadRequest, message);

				return;
			}

			database.Response = debugging
				? database.Request.CreateErrorResponse(HttpStatusCode.BadRequest, database.Exception.Message, database.Exception)
				: database.Request.CreateErrorResponse(HttpStatusCode.BadRequest, database.Exception.Message);
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