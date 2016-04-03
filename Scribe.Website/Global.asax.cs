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
using Scribe.Services;

#endregion

namespace Scribe.Website
{
	public class MvcApplication : HttpApplication
	{
		#region Properties

		public static bool IsConfigured { get; set; }
		public static string PrintCss { get; set; }
		public static string ViewCss { get; set; }

		#endregion

		#region Methods

		protected void Application_BeginRequest()
		{
			var uri = Request.Url.AbsoluteUri.ToLower();

			// This redirect is intercepting bundling and signalr request. Need to fix this better.
			if (!IsConfigured && !uri.Contains("setup") && !uri.Contains("/bundle/") && !uri.Contains("/signalr/"))
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

			GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings = Extensions.GetSerializerSettings();
			GlobalConfiguration.Configuration.Formatters.Remove(GlobalConfiguration.Configuration.Formatters.XmlFormatter);
			GlobalConfiguration.Configuration.EnsureInitialized();

			Database.SetInitializer(new MigrateDatabaseToLatestVersion<ScribeSqlDatabase, Configuration>(true));

			using (var datacontext = new ScribeSqlDatabase())
			{
				var settingsService = new SettingsService(datacontext, null);

				IsConfigured = datacontext.Users.Any();
				PrintCss = settingsService.PrintCss;
				ViewCss = settingsService.ViewCss;
			}
		}

		#endregion
	}
}