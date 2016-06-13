#region References

using System;
using System.Security.Authentication;
using System.Web.Mvc;
using System.Web.Routing;
using Scribe.Exceptions;

#endregion

namespace Scribe.Website.Attributes
{
	public class MvcExceptionFilterAttribute : FilterAttribute, IExceptionFilter
	{
		#region Methods

		public void OnException(ExceptionContext database)
		{
			if (database.Exception is PageNotFoundException)
			{
				database.Result = new RedirectToRouteResult(new RouteValueDictionary(new
				{
					controller = "Error",
					action = "404"
				}));

				database.ExceptionHandled = true;
				database.HttpContext.Response.Clear();
			}

			if (database.Exception is AuthenticationException)
			{
				database.Result = new RedirectToRouteResult(new RouteValueDictionary(new
				{
					controller = "Account",
					action = "Login",
					returnUrl = database.HttpContext.Request.Path
				}));

				database.ExceptionHandled = true;
				database.HttpContext.Response.Clear();
			}

			if (database.Exception is UnauthorizedAccessException)
			{
				database.Result = new RedirectToRouteResult(new RouteValueDictionary(new
				{
					controller = "Account",
					action = "Unauthorized"
				}));

				database.ExceptionHandled = true;
				database.HttpContext.Response.Clear();
			}
		}

		#endregion
	}
}