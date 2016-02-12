﻿#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EasyDataFramework;
using Scribe.Converters;
using Scribe.Extensions;
using Scribe.Models.Views;
using Scribe.Services;

#endregion

namespace Scribe.Models.Entities
{
	/// <summary>
	/// Represents a page. Content store in the page version.
	/// </summary>
	public class Page : Entity
	{
		#region Constructors

		[SuppressMessage("ReSharper", "DoNotCallOverridableMethodsInConstructor")]
		public Page()
		{
			History = new Collection<PageHistory>();
		}

		#endregion

		#region Properties

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
		/// The versions of the pages.
		/// </summary>
		public virtual ICollection<PageHistory> History { get; set; }

		/// <summary>
		/// Flag to indicated this pages has been "soft" deleted.
		/// </summary>
		public bool IsDeleted { get; set; }

		/// <summary>
		/// Gets or sets whether the page is locked for administrator only editing.
		/// </summary>
		public bool IsLocked { get; set; }

		/// <summary>
		/// Gets or sets the user who last modified the page.
		/// </summary>
		public virtual User ModifiedBy { get; set; }

		/// <summary>
		/// Gets or sets the user ID of who last modified the page.
		/// </summary>
		public int ModifiedById { get; set; }

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

		public PageHistoryView ToHistoryView()
		{
			var index = History.Count;

			return new PageHistoryView
			{
				Id = Id,
				Title = Title,
				TitleForLink = PageView.ConvertTitleForLink(Title),
				Versions = History
					.OrderByDescending(x => x.Id)
					.Select(x => new PageHistorySummaryView(index--, x))
					.ToList()
			};
		}

		public PageView ToSummaryView()
		{
			return new PageView
			{
				Id = Id,
				LastModified = DateTime.UtcNow.Subtract(ModifiedOn).ToTimeAgo(),
				ModifiedBy = ModifiedBy.DisplayName,
				ModifiedOn = ModifiedOn,
				Title = Title,
				TitleForLink = PageView.ConvertTitleForLink(Title)
			};
		}

		public PageView ToView(MarkupConverter converter)
		{
			return new PageView
			{
				Id = Id,
				CreatedBy = CreatedBy.DisplayName,
				CreatedOn = CreatedOn.ToShortDateString(),
				EditingBy = EditingOn > DateTime.UtcNow.Subtract(ScribeService.EditingTimeout) ? (EditingBy?.DisplayName ?? string.Empty) : string.Empty,
				Files = new List<FileView>(),
				Html = converter.ToHtml(Text),
				LastModified = DateTime.UtcNow.Subtract(ModifiedOn).ToTimeAgo(),
				ModifiedBy = ModifiedBy.DisplayName,
				ModifiedOn = ModifiedOn,
				Pages = new List<string>(),
				Tags = Tags.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct(),
				Text = Text,
				Title = Title,
				TitleForLink = PageView.ConvertTitleForLink(Title)
			};
		}

		#endregion
	}
}