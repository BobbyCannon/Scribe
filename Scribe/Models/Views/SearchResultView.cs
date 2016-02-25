#region References

using System;
using System.Collections.Generic;

#endregion

namespace Scribe.Models.Views
{
	/// <summary>
	/// Contains data for a single search result from a search query.
	/// </summary>
	public class SearchResultView
	{
		#region Properties

		/// <summary>
		/// The length of the content in bytes.
		/// </summary>
		public int ContentLength { get; set; }

		/// <summary>
		/// The summary of the content (the first 150 characters of text with all HTML removed).
		/// </summary>
		public string ContentSummary { get; set; }

		/// <summary>
		/// The person who created the page.
		/// </summary>
		public string CreatedBy { get; set; }

		/// <summary>
		/// The date the page was created on.
		/// </summary>
		public DateTime CreatedOn { get; set; }

		/// <summary>
		/// The page id
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// The lucene.net score for the search result.
		/// </summary>
		public float Score { get; set; }

		/// <summary>
		/// The page status.
		/// </summary>
		public string Status { get; set; }

		/// <summary>
		/// The tags for the page, in space delimited format.
		/// </summary>
		public IEnumerable<string> Tags { get; set; }

		/// <summary>
		/// The page title.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// The page title for a link.
		/// </summary>
		public string TitleForLink { get; set; }

		#endregion
	}
}