#region References

using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Bloodhound;
using Bloodhound.Models;
using Scribe.Data;
using Scribe.Data.Migrations;
using Scribe.Website.Services.Settings;
using Speedy;
using Database = System.Data.Entity.Database;

#endregion

namespace Scribe.Website
{
	public class MvcApplication : HttpApplication
	{
		#region Fields

		private static readonly string[] _ignoredAnalytics;
		private static readonly string[] _ignoredRequest;

		private Event _event;

		#endregion

		#region Constructors

		static MvcApplication()
		{
			_ignoredRequest = new[] { "setup", "/bundle/", "/signalr/", "/api/" };
			_ignoredAnalytics = new[] { "setup", "/bundle/", "/signalr/" };
		}

		#endregion

		#region Properties

		public static bool IsConfigured { get; set; }
		public static string PrintCss { get; set; }
		public static Tracker Tracker { get; set; }
		public static string ViewCss { get; set; }

		#endregion

		#region Methods

		protected void Application_AuthenticateRequest(object sender, EventArgs e)
		{
			var uri = Request.Url.AbsoluteUri.ToLower();

			if (!uri.ContainsAny(_ignoredAnalytics))
			{
				_event = Tracker?.StartEvent(AnalyticEvents.WebRequest.ToString(),
					new EventValue("URI", uri),
					new EventValue("UrlReferrer", Request.UrlReferrer?.ToString() ?? string.Empty),
					new EventValue("UserHostAddress", Request.UserHostAddress ?? string.Empty),
					new EventValue("UserAgent", Request.UserAgent ?? string.Empty),
					new EventValue("IdentityName", User?.Identity?.Name ?? string.Empty)
					);

				Context.Items["Event"] = _event;
			}
		}

		protected void Application_BeginRequest()
		{
			var uri = Request.Url.AbsoluteUri.ToLower();

			// This redirect is intercepting bundling and signalr request. Need to fix this better.
			if (!IsConfigured && !uri.ContainsAny(_ignoredRequest))
			{
				Response.RedirectToRoute("Setup");
			}
		}

		protected void Application_End(object sender, EventArgs e)
		{
			Tracker?.Dispose();
		}

		protected void Application_EndRequest(object sender, EventArgs e)
		{
			_event?.Complete();
			_event = null;

			if (Request.Path.ToLower().StartsWith("/api/") && (Response.StatusCode == 302))
			{
				Response.StatusCode = 401;
			}
		}

		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
			GlobalConfiguration.Configure(WebApiConfig.Register);
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);

			GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings = Speedy.Extensions.GetSerializerSettings(true, false);
			GlobalConfiguration.Configuration.Formatters.Remove(GlobalConfiguration.Configuration.Formatters.XmlFormatter);
			GlobalConfiguration.Configuration.EnsureInitialized();

			Database.SetInitializer(new MigrateDatabaseToLatestVersion<ScribeSqlDatabase, Configuration>(true));

			var appDataPath = HttpContext.Current.Server.MapPath("~/App_Data");
			var client = new ScribeDataChannel(new ScribeDatabaseProvider(() => new ScribeSqlDatabase()));
			var provider = new KeyValueRepositoryProvider<Event>(appDataPath);

			Tracker = Tracker.Start(client, provider);

			using (var datacontext = new ScribeSqlDatabase())
			{
				var siteSettings = SiteSettings.Load(datacontext);

				IsConfigured = datacontext.Users.Any();
				PrintCss = siteSettings.PrintCss ?? string.Empty;
				ViewCss = siteSettings.ViewCss ?? string.Empty;
			}
		}

		#endregion
	}
}