#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Scribe.Converters;
using Scribe.Extensions;
using Scribe.Models.Entities;
using Scribe.Services;

#endregion

namespace Scribe.Models.Views
{
	public class PageView
	{
		#region Constructors

		public PageView()
		{
			Id = 0;
			EditingBy = string.Empty;
			Files = new List<FileView>();
			Html = string.Empty;
			LastModified = string.Empty;
			ModifiedBy = string.Empty;
			Pages = new List<string>();
			Tags = new List<string>();
			Text = string.Empty;
			Title = string.Empty;
			TitleForLink = string.Empty;
		}

		public PageView(Page page, MarkupConverter converter)
		{
			Id = page.Id;
			EditingBy = page.EditingOn > DateTime.UtcNow.Subtract(PageService.EditingTimeout) ? (page.EditingBy?.DisplayName ?? string.Empty) : string.Empty;
			Files = new List<FileView>();
			Html = converter.ToHtml(page.Text);
			LastModified = DateTime.UtcNow.Subtract(page.ModifiedOn).ToTimeAgo();
			ModifiedBy = page.ModifiedBy.DisplayName;
			Pages = new List<string>();
			Tags = page.Tags.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct();
			Text = page.Text;
			Title = page.Title;
			TitleForLink = ConvertTitleForLink(page.Title);
		}

		#endregion

		#region Properties

		public string EditingBy { get; set; }

		public IEnumerable<FileView> Files { get; set; }

		public string Html { get; set; }

		public int Id { get; set; }

		public string LastModified { get; set; }

		public string ModifiedBy { get; set; }

		public List<string> Pages { get; set; }

		public IEnumerable<string> Tags { get; set; }

		public string Text { get; set; }

		public string Title { get; set; }

		public string TitleForLink { get; set; }

		#endregion

		#region Methods

		public static string ConvertTitleForLink(string title)
		{
			var regex = new Regex("[^a-zA-Z\\d]");
			return HttpUtility.HtmlEncode(regex.Replace(title, ""));
		}

		#endregion
	}
}