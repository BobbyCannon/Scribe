#region References

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using Scribe.Models.Enumerations;

#endregion

namespace Scribe.Models.Views
{
	public class PageView
	{
		#region Constructors

		public PageView()
		{
			ApprovalStatus = ApprovalStatus.None;
			CreatedBy = string.Empty;
			CreatedOn = DateTime.MinValue;
			EditingBy = string.Empty;
			Files = new List<FileView>();
			Html = string.Empty;
			Id = 0;
			IsPublished = false;
			LastModified = string.Empty;
			ModifiedBy = string.Empty;
			ModifiedOn = DateTime.MinValue;
			Pages = new List<string>();
			Tags = new List<string>();
			Text = string.Empty;
			Title = string.Empty;
		}

		#endregion

		#region Properties

		public ApprovalStatus ApprovalStatus { get; set; }

		public string CreatedBy { get; set; }

		public DateTime CreatedOn { get; set; }

		public string EditingBy { get; set; }

		public IEnumerable<FileView> Files { get; set; }

		public string Html { get; set; }

		public int Id { get; set; }

		public bool IsPublished { get; set; }

		public string LastModified { get; set; }

		public string ModifiedBy { get; set; }

		public DateTime ModifiedOn { get; set; }

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
			return HttpUtility.HtmlEncode(regex.Replace(title ?? string.Empty, ""));
		}

		#endregion
	}
}