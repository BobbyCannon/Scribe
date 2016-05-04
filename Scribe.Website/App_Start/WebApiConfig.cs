#region References

using System.Web.Http;
using System.Web.Http.ExceptionHandling;

#endregion

namespace Scribe.Website
{
	public static class WebApiConfig
	{
		#region Methods

		public static void Register(HttpConfiguration config)
		{
			config.MapHttpAttributeRoutes();
			config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{action}/{id}", new { id = RouteParameter.Optional });

			config.Filters.Add(new AuthorizeAttribute());
			config.Filters.Add(new WebApiExceptionFilterAttribute());
			config.Services.Add(typeof(IExceptionLogger), new TraceExceptionLogger());
		}

		#endregion
	}
}