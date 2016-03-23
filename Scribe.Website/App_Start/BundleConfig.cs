#region References

using System.Web.Optimization;

#endregion

namespace Scribe.Website
{
	public static class BundleConfig
	{
		#region Methods

		public static void RegisterBundles(BundleCollection bundles)
		{
			bundles.Add(new ScriptBundle("~/bundle/js").Include(
				"~/Scripts/jquery-{version}.js",
				"~/Scripts/jquery.fieldSelection.js",
				"~/Scripts/jquery.signalR-{version}.js",
				"~/Scripts/underscore.js",
				"~/Scripts/angular.js",
				"~/Scripts/angular-sanitize.js",
				"~/Scripts/Directives/FileUpload.js",
				"~/Scripts/toastr.js",
				"~/Scripts/scribe.js"));

			bundles.Add(new StyleBundle("~/bundle/css").Include(
				"~/Content/font-awesome.css",
				"~/Content/toastr.css",
				"~/Content/scribe.css"));
		}

		#endregion
	}
}