#region References

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Scribe.Converters;
using Scribe.Data;
using Scribe.Models.Entities;
using Scribe.Models.Views;
using Directory = System.IO.Directory;
using LuceneVersion = Lucene.Net.Util.Version;

#endregion

namespace Scribe.Services
{
	/// <summary>
	/// Provides searching tasks using a Lucene.net search index.
	/// </summary>
	public class SearchService
	{
		#region Fields

		private readonly IScribeContext _context;
		private readonly string _indexPath;
		private readonly MarkupConverter _markupConverter;
		private static readonly Regex _removeTagsRegex = new Regex("<(.|\n)*?>");
		private static readonly LuceneVersion LUCENEVERSION = LuceneVersion.LUCENE_29;

		#endregion

		#region Constructors

		public SearchService(IScribeContext context, string path)
		{
			_context = context;
			_markupConverter = new MarkupConverter(context);
			_indexPath = path;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Adds the specified page to the search index.
		/// </summary>
		/// <param name="page"> The page to add. </param>
		public virtual void Add(Page page)
		{
			try
			{
				EnsureDirectoryExists();

				var analyzer = new StandardAnalyzer(LUCENEVERSION);
				using (var writer = new IndexWriter(FSDirectory.Open(new DirectoryInfo(_indexPath)), analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED))
				{
					AddIndex(page, writer);
				}
			}
			catch (FileNotFoundException)
			{
				CreateIndex();
				Add(page);
			}
		}

		/// <summary>
		/// Creates the initial search index based on all pages in the system.
		/// </summary>
		public virtual void CreateIndex()
		{
			EnsureDirectoryExists();

			var analyzer = new StandardAnalyzer(LUCENEVERSION);
			using (var writer = new IndexWriter(FSDirectory.Open(new DirectoryInfo(_indexPath)), analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
			{
				foreach (var page in _context.Pages.ToList())
				{
					AddIndex(page, writer);
				}
			}
		}

		/// <summary>
		/// Deletes the specified page from the search indexes.
		/// </summary>
		/// <param name="page"> The page to remove. </param>
		public virtual int Delete(Page page)
		{
			try
			{
				EnsureDirectoryExists();

				var count = 0;
				using (var reader = IndexReader.Open(FSDirectory.Open(new DirectoryInfo(_indexPath)), false))
				{
					count += reader.DeleteDocuments(new Term("id", page.Id.ToString()));
				}

				return count;
			}
			catch (FileNotFoundException)
			{
				return 0;
			}
		}

		/// <summary>
		/// Searches the lucene index with the search text.
		/// </summary>
		/// <param name="searchText"> The text to search with. </param>
		public virtual SearchView Search(string searchText)
		{
			// This check is for the benefit of the CI builds
			if (!Directory.Exists(_indexPath))
			{
				CreateIndex();
			}

			var response = new SearchView(searchText);

			if (string.IsNullOrWhiteSpace(searchText))
			{
				return response;
			}

			var analyzer = new StandardAnalyzer(LUCENEVERSION);
			var parser = new MultiFieldQueryParser(LuceneVersion.LUCENE_29, new[] { "content", "title", "tag" }, analyzer);

			Query query;
			try
			{
				query = parser.Parse(searchText);
			}
			catch (ParseException)
			{
				// Catch syntax errors in the search and remove them.
				searchText = QueryParser.Escape(searchText);
				query = parser.Parse(searchText);
			}

			if (query != null)
			{
				try
				{
					EnsureDirectoryExists();

					using (var searcher = new IndexSearcher(FSDirectory.Open(new DirectoryInfo(_indexPath)), true))
					{
						var topDocs = searcher.Search(query, 1000);

						foreach (var scoreDoc in topDocs.ScoreDocs)
						{
							var document = searcher.Doc(scoreDoc.Doc);
							response.Results.Add(new SearchResultView(document, scoreDoc));
						}
					}
				}
				catch (FileNotFoundException)
				{
					// For 1.7's change to the Lucene search path.
					CreateIndex();
				}
			}

			return response;
		}

		/// <summary>
		/// Updates the <see cref="Page" /> in the search index, by removing it and re-adding it.
		/// </summary>
		/// <param name="model"> The page to update </param>
		public virtual void Update(Page model)
		{
			EnsureDirectoryExists();
			Delete(model);
			Add(model);
		}

		private void AddIndex(Page model, IndexWriter writer)
		{
			var content = _markupConverter.ToHtml(model.History.OrderByDescending(x => x.Id).First().Text);
			var tags = string.Join(", ", model.Tags.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries));

			var document = new Document();
			document.Add(new Field("id", model.Id.ToString(), Field.Store.YES, Field.Index.ANALYZED));
			document.Add(new Field("content", content, Field.Store.YES, Field.Index.ANALYZED));
			document.Add(new Field("contentsummary", GetContentSummary(content), Field.Store.YES, Field.Index.NO));
			document.Add(new Field("title", model.Title, Field.Store.YES, Field.Index.ANALYZED));
			document.Add(new Field("tags", tags, Field.Store.YES, Field.Index.ANALYZED));
			document.Add(new Field("createdby", model.CreatedBy.DisplayName, Field.Store.YES, Field.Index.NOT_ANALYZED));
			document.Add(new Field("createdon", model.CreatedOn.ToShortDateString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
			document.Add(new Field("contentlength", content.Length.ToString(), Field.Store.YES, Field.Index.NO));

			writer.AddDocument(document);
			writer.Optimize();
		}

		private void EnsureDirectoryExists()
		{
			if (!Directory.Exists(_indexPath))
			{
				Directory.CreateDirectory(_indexPath);
			}
		}

		/// <summary>
		/// Converts the page summary to a lucene Document with the relevant searchable fields.
		/// </summary>
		private string GetContentSummary(string html)
		{
			// Turn the contents into HTML, then strip the tags for the mini summary. This needs some works
			var modelHtml = _removeTagsRegex.Replace(html, "");

			if (modelHtml.Length > 400)
			{
				modelHtml = modelHtml.Substring(0, 399);
			}

			return modelHtml + " ...";
		}

		#endregion
	}
}