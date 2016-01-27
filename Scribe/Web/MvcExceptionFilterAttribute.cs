#region References

using System;
using System.Security.Authentication;
using System.Web.Mvc;
using System.Web.Routing;
using Scribe.Exceptions;

#endregion

namespace Scribe.Web
{
	public class MvcExceptionFilterAttribute : FilterAttribute, IExceptionFilter
	{
		#region Methods

		public void OnException(ExceptionContext context)
		{
			if (context.Exception is PageNotFoundException)
			{
				context.Result = new RedirectToRouteResult(new RouteValueDictionary(new
				{
					controller = "Error",
					action = "404"
				}));

				context.ExceptionHandled = true;
				context.HttpContext.Response.Clear();
			}

			if (context.Exception is AuthenticationException)
			{
				context.Result = new RedirectToRouteResult(new RouteValueDictionary(new
				{
					controller = "Account",
					action = "Login",
					returnUrl = context.HttpContext.Request.Path
				}));

				context.ExceptionHandled = true;
				context.HttpContext.Response.Clear();
			}

			if (context.Exception is UnauthorizedAccessException)
			{
				context.Result = new RedirectToRouteResult(new RouteValueDictionary(new
				{
					controller = "Account",
					action = "Unauthorized"
				}));

				context.ExceptionHandled = true;
				context.HttpContext.Response.Clear();
			}
		}

		#endregion
	}
}