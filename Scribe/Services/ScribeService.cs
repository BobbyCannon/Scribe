#region References

using System;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using Scribe.Converters;
using Scribe.Data;
using Scribe.Exceptions;
using Scribe.Extensions;
using Scribe.Models.Data;
using Scribe.Models.Entities;
using Scribe.Models.Views;

#endregion

namespace Scribe.Services
{
	public class ScribeService : IScribeService
	{
		#region Fields

		private readonly AccountService _accountService;
		private readonly IScribeContext _context;
		private readonly MarkupConverter _converter;
		private readonly ISearchService _searchService;
		private readonly SettingsService _settings;
		private readonly User _user;

		#endregion

		#region Constructors

		public ScribeService(IScribeContext context, AccountService accountService, ISearchService searchService, User user)
		{
			_context = context;
			_accountService = accountService;
			_converter = new MarkupConverter();
			_converter.LinkParsed += (title, title2) => _context.Pages.OrderBy(x => x.Id).Where(x => x.Title == title || x.Title == title2).Select(x => new PageView { Id = x.Id, Title = x.Title }).FirstOrDefault();
			_searchService = searchService;
			_settings = new SettingsService(context, user);
			_user = user;
		}

		static ScribeService()
		{
			EditingTimeout = new TimeSpan(0, 15, 0);
		}

		#endregion

		#region Properties

		public static TimeSpan EditingTimeout { get; }

		#endregion

		#region Methods

		public PageView BeginEditingPage(int id)
		{
			var page = GetPageQuery(x => x.CreatedBy, x => x.ModifiedBy)
				.FirstOrDefault(x => x.Id == id);

			if (page == null)
			{
				throw new PageNotFoundException("Failed to find the page with that ID.");
			}

			var timeoutThreshold = DateTime.UtcNow.Subtract(EditingTimeout);
			if (page.EditingOn <= timeoutThreshold)
			{
				page.EditingBy = _user;
				page.EditingOn = DateTime.UtcNow;
				_context.SaveChanges();
			}

			var response = new PageView(page, _converter);

			response.Files = GetFiles(new PagedRequest { PerPage = int.MaxValue, IncludeDetails = false }).Results;
			response.Pages = GetPages(new PagedRequest { PerPage = int.MaxValue }).Results.Select(x => x.Title).ToList();

			return response;
		}

		public void CancelPage(int id)
		{
			var page = GetPageQuery().FirstOrDefault(x => x.Id == id);
			if (page == null)
			{
				throw new ArgumentException("The page could not be found.", nameof(id));
			}

			page.EditingById = null;
			page.EditingOn = SqlDateTime.MinValue.Value;
			_context.SaveChanges();
		}

		public void DeleteFile(int id)
		{
			var file = _context.Files.FirstOrDefault(x => x.Id == id);
			if (file == null)
			{
				throw new ArgumentException("Failed to find the file with the provided ID.", nameof(id));
			}

			if (_settings.SoftDelete)
			{
				file.IsDeleted = true;
			}
			else
			{
				_context.Files.Remove(file);
			}

			_context.SaveChanges();
		}

		public void DeletePage(int id)
		{
			var page = _context.Pages.FirstOrDefault(x => x.Id == id);
			if (page == null)
			{
				throw new ArgumentException("Failed to find the page with the provided ID.", nameof(id));
			}

			if (_settings.SoftDelete)
			{
				page.IsDeleted = true;
			}
			else
			{
				_context.Pages.Remove(page);
			}

			_context.SaveChanges();
		}

		public void DeleteTag(string tag)
		{
			if (tag.Equals("public", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException("Cannot rename the tag to public.");
			}

			if (string.IsNullOrWhiteSpace(tag))
			{
				throw new ArgumentException("The tag name must be provided.", nameof(tag));
			}

			var name1 = "," + tag + ",";
			var pagesToUpdate = GetPageQuery().Where(x => x.Tags.Contains(name1)).ToList();
			pagesToUpdate.ForEach(page => page.Tags = page.Tags.Replace(name1, ","));

			_context.SaveChanges();

			pagesToUpdate.ForEach(page => _searchService.Update(new PageView(page, _converter)));
		}

		public FileView GetFile(int id, bool includeData = false)
		{
			var file = _context.Files.FirstOrDefault(x => x.Id == id && !x.IsDeleted);
			if (file == null)
			{
				throw new ArgumentException("The file could not be found.", nameof(id));
			}

			return FileView.Create(file, includeData);
		}

		public FileView GetFile(string name, bool includeData = false)
		{
			var file = _context.Files.FirstOrDefault(x => x.Name == name && !x.IsDeleted);
			if (file == null)
			{
				throw new ArgumentException("The file could not be found.", nameof(name));
			}

			return FileView.Create(file, includeData);
		}

		public PagedResults<FileView> GetFiles(PagedRequest request = null)
		{
			var query = _context.Files.Where(x => !x.IsDeleted);
			request = request ?? new PagedRequest();

			if (!string.IsNullOrWhiteSpace(request.Filter))
			{
				query = query.Where(x => x.Name.Contains(request.Filter));
			}

			return GetPagedResults(query, request, x => x.Id, x => new FileView
			{
				Id = x.Id,
				Name = x.Name,
				Size = x.Size / 1024 + " kb",
				Type = x.Type
			});
		}

		public PageView GetFrontPage()
		{
			var page = GetPageQuery()
				.Where(x => x.Tags.Contains(",homepage,"))
				.OrderBy(x => x.Id)
				.ThenBy(x => x.IsLocked)
				.FirstOrDefault();

			return page != null
				? new PageView(page, _converter)
				: new PageView
				{
					Html = "Please set a home page. Just add a new page and give it the tag \"homepage\".",
					Text = "Please set a home page. Just add a new page and give it the tag \"homepage\".",
					Title = "Missing Home Page"
				};
		}

		public PageView GetPage(int id, bool includeHistory = false)
		{
			var page = GetPageQuery(x => x.CreatedBy, x => x.ModifiedBy)
				.FirstOrDefault(x => x.Id == id);

			if (page == null)
			{
				throw new PageNotFoundException("Failed to find the page with that ID.");
			}

			return new PageView(page, _converter);
		}

		public PageDifferenceView GetPageDifference(int id)
		{
			var version = _context.PageVersions
				.Including(x => x.Page, x => x.EditedBy)
				.First(x => x.Id == id);

			if (version == null)
			{
				throw new PageNotFoundException("Failed to find the page with that version ID.");
			}

			var previous = _context.PageVersions.FirstOrDefault(x => x.PageId == version.PageId && x.Id < version.Id);
			var response = new PageDifferenceView
			{
				Id = version.PageId,
				LastModified = DateTime.UtcNow.Subtract(version.EditedOn).ToTimeAgo(),
				ModifiedBy = version.EditedBy.DisplayName,
				Title = version.Page.Title,
				TitleForLink = PageView.ConvertTitleForLink(version.Page.Title)
			};

			if (previous == null)
			{
				response.Html = _converter.ToHtml(version.Text);
			}
			else
			{
				var service = new HtmlDiff(_converter.ToHtml(previous.Text), _converter.ToHtml(version.Text));
				response.Html = service.Build();
			}

			return response;
		}

		public PageHistoryView GetPageHistory(int id)
		{
			var page = GetPageQuery().FirstOrDefault(x => x.Id == id);
			if (page == null)
			{
				throw new PageNotFoundException("Failed to find the page with that ID.");
			}

			return new PageHistoryView(page);
		}

		public PagedResults<PageView> GetPages(PagedRequest request = null)
		{
			var query = GetPageQuery(x => x.ModifiedBy);
			request = request ?? new PagedRequest();

			if (!string.IsNullOrWhiteSpace(request.Filter))
			{
				query = query.Where(x => x.Title.Contains(request.Filter));
			}

			return GetPagedResults(query, request, x => x.Title, x => new PageView
			{
				Id = x.Id,
				LastModified = DateTime.UtcNow.Subtract(x.ModifiedOn).ToTimeAgo(),
				ModifiedBy = x.ModifiedBy.DisplayName,
				ModifiedOn = x.ModifiedOn,
				Title = x.Title,
				TitleForLink = PageView.ConvertTitleForLink(x.Title)
			});
		}

		public PagedResults<PageSummaryView> GetPagesWithTag(PagedRequest request = null)
		{
			request = request ?? new PagedRequest();

			var formattedTag = "," + request.Filter + ",";
			var query = GetPageQuery()
				.Where(x => x.Tags.Contains(formattedTag))
				.Select(x => new { x.Id, x.Title, x.Tags })
				.Where(x => x.Tags.Contains(formattedTag));

			return GetPagedResults(query, request, x => x.Title, x => new PageSummaryView { Id = x.Id, Title = x.Title });
		}

		public PagedResults<TagView> GetTags(PagedRequest request = null)
		{
			request = request ?? new PagedRequest();

			var query = GetPageQuery()
				.Select(x => new { x.Title, x.Tags })
				.ToList()
				.SelectMany(x => x.Tags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				.GroupBy(x => x)
				.AsQueryable();

			return GetPagedResults(query, request, x => x.Key, x => TagView.Create(x.Key, x.Count()));
		}

		public void LogIn(Credentials credentials)
		{
			_accountService.LogIn(credentials);
		}

		public void LogOut()
		{
			_accountService.LogOut();
		}

		public string Preview(PageView model)
		{
			if (model.Id > 0)
			{
				UpdateEditingPage(model);
				_context.SaveChanges();
			}

			return _converter.ToHtml(model.Text);
		}

		public void RenameTag(RenameValues values)
		{
			if (values.OldName.Equals("public", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException("Cannot rename the public tag.");
			}

			if (values.NewName.Equals("public", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException("Cannot rename the tag to public.");
			}

			if (string.IsNullOrWhiteSpace(values.OldName))
			{
				throw new ArgumentException("The old name must be provided.", nameof(values));
			}

			if (string.IsNullOrWhiteSpace(values.NewName))
			{
				throw new ArgumentException("The new name must be provided.", nameof(values));
			}

			var name1 = "," + values.OldName + ",";
			var name2 = "," + values.NewName + ",";
			var pagesToUpdate = GetPageQuery().Where(x => x.Tags.Contains(name1)).ToList();

			pagesToUpdate.ForEach(page => page.Tags = page.Tags.Replace(name1, name2));

			_context.SaveChanges();

			pagesToUpdate.ForEach(page => _searchService.Update(new PageView(page, _converter)));
		}

		public int SaveFile(FileView view)
		{
			var file = _context.Files.FirstOrDefault(x => x.Name == view.Name)
				?? new File { Name = view.Name, CreatedBy = _user, CreatedOn = DateTime.UtcNow };

			if (!_settings.OverwriteFilesOnUpload && file.Id != 0)
			{
				throw new InvalidOperationException("The file already exists and cannot be overwritten.");
			}

			file.Data = view.Data;
			file.Size = view.Data.Length;
			file.Type = view.Type;
			file.ModifiedOn = file.Id == 0 ? file.CreatedOn : DateTime.UtcNow;
			file.ModifiedBy = _user;

			_context.Files.AddOrUpdate(file);
			_context.SaveChanges();

			return file.Id;
		}

		public PageView SavePage(PageView view)
		{
			var page = GetPageQuery().FirstOrDefault(x => x.Id == view.Id)
				?? new Page { CreatedOn = DateTime.UtcNow, CreatedBy = _user, IsLocked = false };

			if (page.EditingById != null && page.EditingById != _user?.Id)
			{
				throw new InvalidOperationException("This page is currently being edited by another user.");
			}

			if (page.Id == view.Id && page.Title == view.Title && page.Text == view.Text && page.Tags == $",{string.Join(",", view.Tags)},")
			{
				return null;
			}

			if (page.Text != view.Text)
			{
				var version = new PageHistory
				{
					Text = view.Text,
					EditedBy = _user,
					EditedOn = DateTime.UtcNow
				};

				page.History.Add(version);
				page.Text = view.Text;
				page.ModifiedBy = version.EditedBy;
				page.ModifiedOn = version.EditedOn;
			}

			page.EditingById = null;
			page.EditingOn = SqlDateTime.MinValue.Value;
			page.Tags = $",{string.Join(",", view.Tags.Select(x => x.Trim()).Distinct())},";
			page.Title = view.Title;

			_context.Pages.AddOrUpdate(page);
			_context.SaveChanges();

			return new PageView(page, _converter);
		}

		public void UpdateEditingPage(PageView model)
		{
			var page = GetPageQuery().FirstOrDefault(x => x.Id == model.Id);
			if (page == null)
			{
				throw new ArgumentException("The page could not be found.", nameof(model));
			}

			page.EditingOn = DateTime.UtcNow;
		}

		private PagedResults<T2> GetPagedResults<T1, T2>(IQueryable<T1> query, PagedRequest request, Func<T1, object> orderBy, Func<T1, T2> transform)
		{
			var response = new PagedResults<T2>();
			response.Filter = request.Filter;
			response.TotalCount = query.Count();
			response.PerPage = request.PerPage;
			response.TotalPages = response.TotalCount / response.PerPage + (response.TotalCount % response.PerPage > 0 ? 1 : 0);
			response.Page = response.TotalPages < request.Page ? response.TotalPages : request.Page;
			response.Results = query
				.OrderBy(orderBy)
				.Skip((response.Page - 1) * response.PerPage)
				.Take(response.PerPage)
				.ToList()
				.Select(transform)
				.ToList();

			return response;
		}

		private IQueryable<Page> GetPageQuery(params Expression<Func<Page, object>>[] includes)
		{
			var query = includes != null ? _context.Pages.Including(includes) : _context.Pages;
			query = query.Where(x => !x.IsDeleted);

			if (_user == null && _settings.EnablePublicTag)
			{
				query = query.Where(x => x.Tags.Contains(",public,"));
			}

			return query;
		}

		#endregion
	}
}