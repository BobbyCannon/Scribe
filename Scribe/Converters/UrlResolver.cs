#region References

using System.Web;
using Scribe.Models.Views;

#endregion

namespace Scribe.Converters
{
	public class UrlResolver
	{
		#region Methods

		/// <summary>
		/// Gets the internal URL of a page based on the page title.
		/// </summary>
		/// <param name="id"> The page id </param>
		/// <param name="title"> The title of the page </param>
		/// <returns> An absolute path to the page. </returns>
		public virtual string GetInternalUrlForTitle(int id, string title)
		{
			return $"/Page/{id}/{PageView.ConvertTitleForLink(title)}";
		}

		/// <summary>
		/// Gets a URL to the new page resource, appending the title to the query string.
		/// For example /Pages/NewPage?suggestedTitle=xyz
		/// </summary>
		/// <param name="title"> </param>
		/// <returns> </returns>
		public virtual string GetNewPageUrlForTitle(string title)
		{
			return $"/NewPage?suggestedTitle={HttpUtility.HtmlEncode(title)}";
		}

		#endregion
	}
}