#region References

using System;
using System.Collections.Generic;
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
using Scribe.Services;
using Directory = System.IO.Directory;
using LuceneVersion = Lucene.Net.Util.Version;

#endregion

namespace Scribe.Website.Services
{
	/// <summary>
	/// Provides searching tasks using a Lucene.net search index.
	/// </summary>
	public class SearchService : ISearchService
	{
		#region Fields

		private readonly IScribeContext _context;
		private readonly MarkupConverter _converter;
		private readonly string _indexPath;
		private static readonly Regex _removeTagsRegex = new Regex("<(.|\n)*?>");
		private readonly SettingsService _settings;
		private readonly User _user;
		private static readonly LuceneVersion _luceneversion = LuceneVersion.LUCENE_30;

		#endregion

		#region Constructors

		public SearchService(IScribeContext context, string path, User user)
		{
			_context = context;
			_converter = new MarkupConverter(context);
			_settings = new SettingsService(context, user);
			_indexPath = path;
			_user = user;
		}

		#endregion

		#region Properties

		public static string SearchPath { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Adds the specified page to the search index.
		/// </summary>
		/// <param name="page"> The page to add. </param>
		public virtual void Add(PageView page)
		{
			try
			{
				EnsureDirectoryExists();

				var analyzer = new StandardAnalyzer(_luceneversion);
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

		public SearchResultView Create(Document document, ScoreDoc scoreDoc)
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
			var createdOn = DateTime.UtcNow;
			if (!DateTime.TryParse(document.GetField("createdon").StringValue, out createdOn))
			{
				createdOn = DateTime.UtcNow;
			}

			var title = document.GetField("title").StringValue;

			return new SearchResultView
			{
				Id = int.Parse(document.GetField("id").StringValue),
				Title = title,
				TitleForLink = PageView.ConvertTitleForLink(title),
				ContentSummary = document.GetField("contentsummary").StringValue,
				Tags = document.GetField("tags").StringValue.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Distinct(),
				CreatedBy = document.GetField("createdby").StringValue,
				ContentLength = int.Parse(document.GetField("contentlength").StringValue),
				Score = scoreDoc.Score,
				CreatedOn = createdOn
			};
		}

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

		/// <summary>
		/// Creates the initial search index based on all pages in the system.
		/// </summary>
		public virtual void CreateIndex()
		{
			EnsureDirectoryExists();

			var analyzer = new StandardAnalyzer(_luceneversion);
			using (var writer = new IndexWriter(FSDirectory.Open(new DirectoryInfo(_indexPath)), analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
			{
				foreach (var page in _context.Pages.ToList())
				{
					AddIndex(new PageView(page, _converter), writer);
				}
			}
		}

		/// <summary>
		/// Deletes the specified page from the search indexes.
		/// </summary>
		/// <param name="page"> The page to remove. </param>
		public virtual int Delete(PageView page)
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

			var analyzer = new StandardAnalyzer(_luceneversion);
			var parser = new MultiFieldQueryParser(_luceneversion, new[] { "content", "title", "tag" }, analyzer);

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
							var result = Create(document, scoreDoc);

							if (_user == null && _settings.EnablePublicTag && !result.Tags.Contains("public"))
							{
								continue;
							}

							response.Results.Add(result);
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
		public virtual void Update(PageView model)
		{
			EnsureDirectoryExists();
			Delete(model);
			Add(model);
		}

		private void AddIndex(PageView model, IndexWriter writer)
		{
			var content = model.Html;
			var tags = string.Join(", ", model.Tags);

			var document = new Document();
			document.Add(new Field("id", model.Id.ToString(), Field.Store.YES, Field.Index.ANALYZED));
			document.Add(new Field("content", content, Field.Store.YES, Field.Index.ANALYZED));
			document.Add(new Field("contentsummary", GetContentSummary(content), Field.Store.YES, Field.Index.NO));
			document.Add(new Field("title", model.Title, Field.Store.YES, Field.Index.ANALYZED));
			document.Add(new Field("tags", tags, Field.Store.YES, Field.Index.ANALYZED));
			document.Add(new Field("createdby", model.CreatedBy, Field.Store.YES, Field.Index.NOT_ANALYZED));
			document.Add(new Field("createdon", model.CreatedOn, Field.Store.YES, Field.Index.NOT_ANALYZED));
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