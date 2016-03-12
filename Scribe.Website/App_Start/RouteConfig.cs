#region References

using System.Web.Mvc;
using System.Web.Routing;

#endregion

namespace Scribe.Website
{
	public static class RouteConfig
	{
		#region Methods

		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapRoute("Login", "Login", new { controller = "Account", action = "Login", returnUrl = UrlParameter.Optional });
			routes.MapRoute("Users", "Users", new { controller = "Account", action = "Users" });
			routes.MapRoute("User", "User/{id}", new { controller = "Account", action = "User", id = UrlParameter.Optional });
			routes.MapRoute("UsersWithTag", "UsersWithTag/{tag}", new { controller = "Account", action = "UsersWithTag", tag = UrlParameter.Optional });
			routes.MapRoute("Unauthorized", "Unauthorized", new { controller = "Account", action = "Unauthorized" });
			routes.MapRoute("Tags", "Tags", new { controller = "Page", action = "Tags" });
			routes.MapRoute("Setup", "Setup", new { controller = "Page", action = "Setup" });
			routes.MapRoute("About", "About", new { controller = "Page", action = "About" });
			routes.MapRoute("Manual", "Manual", new { controller = "Page", action = "Manual" });
			routes.MapRoute("RenameTag", "RenameTag", new { controller = "Page", action = "RenameTag" });
			routes.MapRoute("GetPreview", "GetPreview", new { controller = "Page", action = "Preview" });
			routes.MapRoute("EditPage", "EditPage/{id}/{title}", new { controller = "Page", action = "Edit", id = UrlParameter.Optional, title = UrlParameter.Optional });
			routes.MapRoute("NewPage", "NewPage/{suggestedTitle}", new { controller = "Page", action = "New", suggestedTitle = UrlParameter.Optional });
			routes.MapRoute("CancelEdit", "CancelEdit/{id}", new { controller = "Page", action = "CancelEdit", id = UrlParameter.Optional });
			routes.MapRoute("Wiki", "Wiki/{id}/{title}", new { controller = "Page", action = "Page", title = UrlParameter.Optional });
			routes.MapRoute("PageDifference", "PageDifference/{id}/{title}", new { controller = "Page", action = "Difference", title = UrlParameter.Optional });
			routes.MapRoute("PageHistory", "PageHistory/{id}/{title}", new { controller = "Page", action = "History", title = UrlParameter.Optional });
			routes.MapRoute("Page", "Page/{id}/{title}", new { controller = "Page", action = "Page", title = UrlParameter.Optional });
			routes.MapRoute("Pages", "Pages", new { controller = "Page", action = "Pages" });
			routes.MapRoute("PagesWithTag", "PagesWithTag/{tag}", new { controller = "Page", action = "PagesWithTag", tag = UrlParameter.Optional });
			routes.MapRoute("Profile", "Profile", new { controller = "Account", action = "Profile" });
			routes.MapRoute("Search", "Search", new { controller = "Page", action = "Search", term = UrlParameter.Optional });
			routes.MapRoute("Settings", "Settings", new { controller = "Page", action = "Settings" });
			routes.MapRoute("File", "File/{id}/{name}", new { controller = "File", action = "File", name = UrlParameter.Optional });
			routes.MapRoute("Files", "Files", new { controller = "File", action = "Files" });
			routes.MapRoute("Default", "{controller}/{action}/{id}", new { controller = "Page", action = "Home", id = UrlParameter.Optional });
		}

		#endregion
	}
}