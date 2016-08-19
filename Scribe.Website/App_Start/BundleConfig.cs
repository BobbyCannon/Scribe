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
				"~/Scripts/Chart.js",
				"~/Scripts/linq.js",
				"~/Scripts/Directives/FileUpload.js",
				"~/Scripts/pickadate/picker.js",
				"~/Scripts/pickadate/picker.date.js",
				"~/Scripts/pickadate/picker.time.js",
				"~/Scripts/toastr.js",
				"~/Scripts/scribe.js",
				"~/Scripts/scribe.md5.js"));

			bundles.Add(new StyleBundle("~/bundle/css").Include(
				"~/Scripts/pickadate/themes/classic.css",
				"~/Scripts/pickadate/themes/classic.date.css",
				"~/Scripts/pickadate/themes/classic.time.css",
				"~/Content/font-awesome.css",
				"~/Content/toastr.css",
				"~/Content/scribe.css"));
		}

		#endregion
	}
}