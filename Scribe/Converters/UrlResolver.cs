#region References

using System.Web;
using System.Web.Mvc;
using Scribe.Models.Views;

#endregion

namespace Scribe.Converters
{
	public class UrlResolver
	{
		#region Fields

		private readonly HttpContextBase _httpContext;

		#endregion

		#region Constructors

		public UrlResolver(HttpContextBase httpContext = null)
		{
			_httpContext = httpContext;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the internal URL of a page based on the page title.
		/// </summary>
		/// <param name="id"> The page id </param>
		/// <param name="title"> The title of the page </param>
		/// <returns> An absolute path to the page. </returns>
		public virtual string GetInternalUrlForTitle(int id, string title)
		{
			if (_httpContext == null)
			{
				return $"/Page/{id}/{PageView.ConvertTitleForLink(title)}";
			}

			var helper = new UrlHelper(HttpContext.Current.Request.RequestContext);
			return helper.Action("Page", "Page", new { id, title = PageView.ConvertTitleForLink(title) });
		}

		/// <summary>
		/// Gets a URL to the new page resource, appending the title to the query string.
		/// For example /Pages/NewPage?suggestedTitle=xyz
		/// </summary>
		/// <param name="title"> </param>
		/// <returns> </returns>
		public virtual string GetNewPageUrlForTitle(string title)
		{
			if (_httpContext == null)
			{
				return $"/NewPage?suggestedTitle={HttpUtility.HtmlEncode(title)}";
			}

			var helper = new UrlHelper(HttpContext.Current.Request.RequestContext);
			return helper.Action("New", "Page", new { suggestedTitle = HttpUtility.HtmlEncode(title) });
		}

		#endregion
	}
}