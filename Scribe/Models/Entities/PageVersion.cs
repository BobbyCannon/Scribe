#region References

using System;
using System.Collections.Generic;
using System.Linq;
using EasyDataFramework;
using Scribe.Converters;
using Scribe.Models.Enumerations;
using Scribe.Models.Views;
using Scribe.Services;

#endregion

namespace Scribe.Models.Entities
{
	/// <summary>
	/// Represents a page. Content store in the page version.
	/// </summary>
	public class PageVersion : Entity
	{
		#region Properties

		/// <summary>
		/// Gets or sets the approval status of the page.
		/// </summary>
		public ApprovalStatus ApprovalStatus { get; set; }

		/// <summary>
		/// Gets or sets the user who created the page.
		/// </summary>
		public virtual User CreatedBy { get; set; }

		/// <summary>
		/// Gets or sets the user ID for who created the page.
		/// </summary>
		public int CreatedById { get; set; }

		/// <summary>
		/// Gets or sets the user who current editing the page.
		/// </summary>
		public virtual User EditingBy { get; set; }

		/// <summary>
		/// Gets or sets the user ID for who editing the page.
		/// </summary>
		public int? EditingById { get; set; }

		/// <summary>
		/// Gets or sets the date the page was last editing on.
		/// </summary>
		public DateTime EditingOn { get; set; }

		/// <summary>
		/// Gets or sets a flag indicating the page is published.
		/// </summary>
		public bool IsPublished { get; set; }

		/// <summary>
		/// Gets or sets the page.
		/// </summary>
		public virtual Page Page { get; set; }

		/// <summary>
		/// Gets or sets the ID of the page.
		/// </summary>
		public int PageId { get; set; }

		/// <summary>
		/// Gets or sets the tags for the page, in the format ",tag1,tag2,tag3," (no spaces between tags).
		/// </summary>
		public string Tags { get; set; }

		/// <summary>
		/// The current markdown text for the page.
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// Gets or sets the title.
		/// </summary>
		public string Title { get; set; }

		#endregion

		#region Methods

		public static IEnumerable<string> SplitTags(string tags)
		{
			return tags.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct().OrderBy(x => x).ToArray();
		}

		public PageHistorySummaryView ToHistorySummaryView(int number)
		{
			return new PageHistorySummaryView
			{
				Id = Id,
				ApprovalStatus = ApprovalStatus,
				CreatedBy = CreatedBy.DisplayName,
				CreatedOn = CreatedOn,
				IsPublished = IsPublished,
				LastModified = DateTime.UtcNow.Subtract(ModifiedOn).ToTimeAgo(),
				Number = number
			};
		}

		public PageHistoryView ToHistoryView(bool guestView)
		{
			var versionQuery = Page.Versions.Where(x => x.Id <= Id);

			if (guestView)
			{
				versionQuery = versionQuery.Where(x => x.IsPublished && x.ApprovalStatus == ApprovalStatus.Approved);
			}

			var versions = versionQuery.OrderByDescending(x => x.Id).ToList();
			var index = versions.Count;
			var history = versions.Select(x => x.ToHistorySummaryView(index--)).ToList();

			return new PageHistoryView
			{
				Id = PageId,
				Title = Title,
				TitleForLink = PageView.ConvertTitleForLink(Title),
				Versions = history
			};
		}

		public PageView ToView(MarkupConverter converter = null, bool includeDetails = true)
		{
			return new PageView
			{
				ApprovalStatus = ApprovalStatus,
				Id = PageId,
				CreatedBy = CreatedBy.DisplayName,
				CreatedOn = CreatedOn,
				EditingBy = EditingOn > DateTime.UtcNow.Subtract(ScribeService.EditingTimeout) ? (EditingBy?.DisplayName ?? string.Empty) : string.Empty,
				Files = new List<FileView>(),
				Html = includeDetails ? converter?.ToHtml(Text) ?? string.Empty : string.Empty,
				IsHomePage = Page.IsHomePage,
				IsPublished = IsPublished,
				LastModified = DateTime.UtcNow.Subtract(CreatedOn).ToTimeAgo(),
				Pages = new List<string>(),
				Tags = SplitTags(Tags),
				Text = includeDetails ? Text : string.Empty,
				Title = Title,
				TitleForLink = PageView.ConvertTitleForLink(Title)
			};
		}

		#endregion
	}
}