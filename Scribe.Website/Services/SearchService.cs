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
using Scribe.Data;
using Scribe.Data.Entities;
using Scribe.Models.Data;
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

		private readonly IScribeDatabase _database;
		private readonly string _indexPath2;
		private static readonly LuceneVersion _luceneversion = LuceneVersion.LUCENE_30;
		private static readonly Regex _removeTagsRegex = new Regex("<(.|\n)*?>");
		private readonly SettingsService _settings;
		private readonly User _user;

		#endregion

		#region Constructors

		public SearchService(IScribeDatabase database, string path, User user)
		{
			_database = database;
			_settings = new SettingsService(database, user);
			_indexPath2 = path;
			_user = user;
		}

		#endregion

		#region Properties

		public string PrivateSearchPath => _indexPath2 + "\\Private";

		public string PublicSearchPath => _indexPath2 + "\\Public";

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
				AddIndex(PublicSearchPath, false, page);
				AddIndex(PrivateSearchPath, false, page);
			}
			catch (FileNotFoundException)
			{
				Initialize();
				Add(page);
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
				DeleteIndex(PrivateSearchPath, page);
				DeleteIndex(PublicSearchPath, page);
				return 1;
			}
			catch (FileNotFoundException)
			{
				return 0;
			}
		}

		/// <summary>
		/// Creates the initial search index based on all pages in the system.
		/// </summary>
		public virtual void Initialize()
		{
			EnsureDirectoryExists();

			var user = _database.Users.First();
			var privateService = new ScribeService(_database, null, null, user);
			privateService.Converter.ClearEvents();
			AddIndex(PrivateSearchPath, true, privateService.GetPages(new PagedRequest { PerPage = int.MaxValue }).Results.ToArray());

			var publicService = new ScribeService(_database, null, null, null);
			publicService.Converter.ClearEvents();
			AddIndex(PublicSearchPath, true, publicService.GetPages(new PagedRequest { PerPage = int.MaxValue }).Results.ToArray());
		}

		/// <summary>
		/// Searches the lucene index with the search text.
		/// </summary>
		/// <param name="searchText"> The text to search with. </param>
		public virtual SearchView Search(string searchText)
		{
			// This check is for the benefit of the CI builds
			var path = _settings.EnableGuestMode && _user == null ? PublicSearchPath : PrivateSearchPath;
			if (!Directory.Exists(path))
			{
				Initialize();
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

					using (var searcher = new IndexSearcher(FSDirectory.Open(new DirectoryInfo(path)), true))
					{
						var topDocs = searcher.Search(query, 1000);

						foreach (var scoreDoc in topDocs.ScoreDocs)
						{
							var document = searcher.Doc(scoreDoc.Doc);
							var result = Create(document, scoreDoc);
							response.Results.Add(result);
						}
					}
				}
				catch (FileNotFoundException)
				{
					// For 1.7's change to the Lucene search path.
					Initialize();
				}
			}

			return response;
		}

		private void AddIndex(string path, bool newIndex = false, params PageView[] pages)
		{
			var analyzer = new StandardAnalyzer(_luceneversion);
			using (var writer = new IndexWriter(FSDirectory.Open(new DirectoryInfo(path)), analyzer, newIndex, IndexWriter.MaxFieldLength.UNLIMITED))
			{
				foreach (var page in pages)
				{
					DeleteIndex(page, writer);
					AddIndex(page, writer);
				}
			}
		}

		private void AddIndex(PageView model, IndexWriter writer)
		{
			var content = model.Html;
			var tags = string.Join(",", model.Tags);

			var document = new Document();
			document.Add(new Field("content", content, Field.Store.YES, Field.Index.ANALYZED));
			document.Add(new Field("contentlength", content.Length.ToString(), Field.Store.YES, Field.Index.NO));
			document.Add(new Field("contentsummary", GetContentSummary(content), Field.Store.YES, Field.Index.NO));
			document.Add(new Field("createdby", model.CreatedBy, Field.Store.YES, Field.Index.NOT_ANALYZED));
			document.Add(new Field("createdon", model.CreatedOn.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
			document.Add(new Field("id", model.Id.ToString(), Field.Store.YES, Field.Index.ANALYZED));
			document.Add(new Field("tags", tags, Field.Store.YES, Field.Index.ANALYZED));
			document.Add(new Field("title", model.Title, Field.Store.YES, Field.Index.ANALYZED));

			writer.AddDocument(document);
			writer.Optimize();
		}

		private SearchResultView Create(Document document, ScoreDoc scoreDoc)
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
				ContentLength = int.Parse(document.GetField("contentlength").StringValue),
				ContentSummary = document.GetField("contentsummary").StringValue,
				CreatedBy = document.GetField("createdby").StringValue,
				CreatedOn = createdOn,
				Id = int.Parse(document.GetField("id").StringValue),
				Score = scoreDoc.Score,
				Tags = PageVersion.SplitTags(document.GetField("tags").StringValue),
				Title = title
			};
		}

		private void DeleteIndex(string path, params PageView[] pages)
		{
			var analyzer = new StandardAnalyzer(_luceneversion);
			using (var writer = new IndexWriter(FSDirectory.Open(new DirectoryInfo(path)), analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED))
			{
				foreach (var page in pages)
				{
					DeleteIndex(page, writer);
				}
			}
		}

		private void DeleteIndex(PageView page, IndexWriter writer)
		{
			writer.DeleteDocuments(new Term("id", page.Id.ToString()));
		}

		private void EnsureDirectoryExists()
		{
			if (!Directory.Exists(PrivateSearchPath))
			{
				Directory.CreateDirectory(PrivateSearchPath);
			}

			if (!Directory.Exists(PublicSearchPath))
			{
				Directory.CreateDirectory(PublicSearchPath);
			}
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
			EnsureFieldExists(fields, "content");
			EnsureFieldExists(fields, "contentlength");
			EnsureFieldExists(fields, "contentsummary");
			EnsureFieldExists(fields, "createdby");
			EnsureFieldExists(fields, "createdon");
			EnsureFieldExists(fields, "id");
			EnsureFieldExists(fields, "title");
			EnsureFieldExists(fields, "tags");
		}

		/// <summary>
		/// Converts the page summary to a lucene Document with the relevant searchable fields.
		/// </summary>
		private string GetContentSummary(string html)
		{
			// Turn the contents into HTML, then strip the tags for the mini summary. This needs some work.
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