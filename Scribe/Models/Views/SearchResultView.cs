#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Search;

#endregion

namespace Scribe.Models.Views
{
	/// <summary>
	/// Contains data for a single search result from a search query.
	/// </summary>
	public class SearchResultView
	{
		#region Constructors

		public SearchResultView()
		{
		}

		public SearchResultView(Document document, ScoreDoc scoreDoc)
		{
			if (document == null)
			{
				throw new ArgumentNullException(nameof(document));
			}

			if (scoreDoc == null)
			{
				throw new ArgumentNullException(nameof(scoreDoc));
			}

			EnsureFieldsExist(document);

			Id = int.Parse(document.GetField("id").StringValue);
			Title = document.GetField("title").StringValue;
			TitleForLink = PageView.ConvertTitleForLink(Title);
			ContentSummary = document.GetField("contentsummary").StringValue;
			Tags = document.GetField("tags").StringValue.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Distinct();
			CreatedBy = document.GetField("createdby").StringValue;
			ContentLength = int.Parse(document.GetField("contentlength").StringValue);
			Score = scoreDoc.Score;

			var createdOn = DateTime.UtcNow;
			if (!DateTime.TryParse(document.GetField("createdon").StringValue, out createdOn))
			{
				createdOn = DateTime.UtcNow;
			}

			CreatedOn = createdOn;
		}

		#endregion

		#region Properties

		/// <summary>
		/// The length of the content in bytes.
		/// </summary>
		public int ContentLength { get; internal set; }

		/// <summary>
		/// The summary of the content (the first 150 characters of text with all HTML removed).
		/// </summary>
		public string ContentSummary { get; internal set; }

		/// <summary>
		/// The person who created the page.
		/// </summary>
		public string CreatedBy { get; internal set; }

		/// <summary>
		/// The date the page was created on.
		/// </summary>
		public DateTime CreatedOn { get; internal set; }

		/// <summary>
		/// The page id
		/// </summary>
		public int Id { get; internal set; }

		/// <summary>
		/// The lucene.net score for the search result.
		/// </summary>
		public float Score { get; internal set; }

		/// <summary>
		/// The tags for the page, in space delimited format.
		/// </summary>
		public IEnumerable<string> Tags { get; internal set; }

		/// <summary>
		/// The page title.
		/// </summary>
		public string Title { get; internal set; }

		/// <summary>
		/// The page title for a link.
		/// </summary>
		public string TitleForLink { get; set; }

		#endregion

		#region Methods

		private void EnsureFieldExists(IList<IFieldable> fields, string fieldname)
		{
			if (fields.Any(x => x.Name == fieldname) == false)
			{
				throw new Exception("The LuceneDocument did not contain the expected field " + fieldname + ".");
			}
		}

		private void EnsureFieldsExist(Document document)
		{
			var fields = document.GetFields();
			EnsureFieldExists(fields, "id");
			EnsureFieldExists(fields, "title");
			EnsureFieldExists(fields, "contentsummary");
			EnsureFieldExists(fields, "tags");
			EnsureFieldExists(fields, "createdby");
			EnsureFieldExists(fields, "contentlength");
			EnsureFieldExists(fields, "createdon");
		}

		#endregion
	}
}