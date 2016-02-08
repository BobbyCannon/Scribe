#region References

using System.Collections.Generic;
using System.Linq;
using System.Web;
using MarkR;
using Scribe.Data;

#endregion

namespace Scribe.Converters
{
	/// <summary>
	/// A factory class for converting the system's markup syntax to HTML.
	/// </summary>
	public class MarkupConverter
	{
		#region Fields

		private readonly IScribeContext _context;
		private readonly List<string> _externalLinkPrefixes;
		private readonly Markdown _parser;
		private readonly UrlResolver _urlResolver;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new markdown parser which handles the image and link parsing.
		/// </summary>
		public MarkupConverter(IScribeContext context)
		{
			_context = context;
			_externalLinkPrefixes = new List<string> { "http://", "https://", "www.", "mailto:", "#" };
			_parser = new Markdown();
			_parser.LinkParsed += LinkParsed;
			_parser.ImageParsed += ImageParsed;
			_urlResolver = new UrlResolver();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Turns the markdown text provided into HTML.
		/// </summary>
		/// <param name="text"> A markdown text string. </param>
		/// <returns> The markup text converted to HTML. </returns>
		public string ToHtml(string text)
		{
			var html = _parser.Transform(text);
			var tokenParser = new CustomTokenParser();
			return tokenParser.ReplaceTokensAfterParse(html);
		}

		/// <summary>
		/// Adds the attachments folder as a prefix to all image URLs before the HTML <img /> tag is written.
		/// </summary>
		private void ImageParsed(object sender, ImageEventArgs e)
		{
			if (!e.OriginalSrc.StartsWith("http://") && !e.OriginalSrc.StartsWith("https://"))
			{
				e.Src = "/file?name=" + HttpUtility.HtmlEncode(e.OriginalSrc);
			}
		}

		/// <summary>
		/// Handles internal links.
		/// </summary>
		private void LinkParsed(object sender, LinkEventArgs e)
		{
			if (_externalLinkPrefixes.Any(x => e.OriginalHref.StartsWith(x)))
			{
				// Add the external-link class to all outward bound links, 
				// except for anchors pointing to <a name=""> tags on the current page.
				if (!e.OriginalHref.StartsWith("#"))
				{
					e.CssClass = "external-link";
				}

				e.Target = "_blank";
				return;
			}

			// Parse internal links
			var href = e.OriginalHref;
			var title = href;
			var modifiedTitle = title.Replace("-", " ");

			// Find the page, or if it doesn't exist point to the new page URL
			var page = _context.Pages.OrderBy(x => x.Id).FirstOrDefault(x => x.Title == title || x.Title == modifiedTitle);
			if (page != null)
			{
				href = _urlResolver.GetInternalUrlForTitle(page.Id, page.Title);
			}
			else
			{
				href = _urlResolver.GetNewPageUrlForTitle(href);
				e.CssClass = "missing-page-link";
			}

			e.Href = href;
			e.Target = "";
			e.IsInternalLink = true;
		}

		#endregion
	}
}