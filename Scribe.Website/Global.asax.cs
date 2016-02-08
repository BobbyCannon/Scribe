#region References

using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Scribe.Data;
using Scribe.Data.Migrations;
using Scribe.Extensions;

#endregion

namespace Scribe.Website
{
	public class MvcApplication : HttpApplication
	{
		#region Properties

		public static bool IsConfigured { get; set; }

		#endregion

		#region Methods

		protected void Application_BeginRequest()
		{
			var uri = Request.Url.AbsoluteUri.ToLower();

			if (!IsConfigured && !uri.Contains("setup"))
			{
				Response.RedirectToRoute("Setup");
			}
		}

		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
			GlobalConfiguration.Configure(WebApiConfig.Register);
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);

			GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings = ObjectExtensions.GetSerializerSettings();
			GlobalConfiguration.Configuration.Formatters.Remove(GlobalConfiguration.Configuration.Formatters.XmlFormatter);
			GlobalConfiguration.Configuration.EnsureInitialized();

			Database.SetInitializer(new MigrateDatabaseToLatestVersion<ScribeContext, Configuration>(true));

			using (var datacontext = new ScribeContext())
			{
				IsConfigured = datacontext.Users.Any();
			}
		}

		#endregion
	}
}