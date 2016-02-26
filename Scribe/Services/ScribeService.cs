#region References

using System;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using Scribe.Converters;
using Scribe.Data;
using Scribe.Exceptions;
using Scribe.Models.Data;
using Scribe.Models.Entities;
using Scribe.Models.Enumerations;
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
		private readonly SettingsService _settingsService;
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
			_settingsService = new SettingsService(context, user);
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
			VerifyAccess("You must be authenticate to begin editing page.");

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

			var response = page.ToView(_converter);

			response.Files = GetFiles(new PagedRequest { PerPage = int.MaxValue, IncludeDetails = false }).Results;
			response.Pages = GetPages(new PagedRequest { PerPage = int.MaxValue }).Results.Select(x => x.Title).ToList();

			return response;
		}

		public void CancelPage(int id)
		{
			VerifyAccess("You must be authenticate to cancel editing page.");

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
			VerifyAccess("You must be authenticate to delete a file.");

			var file = _context.Files.FirstOrDefault(x => x.Id == id);
			if (file == null)
			{
				throw new ArgumentException("Failed to find the file with the provided ID.", nameof(id));
			}

			if (_settingsService.SoftDelete)
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
			VerifyAccess("You must be authenticate to delete a page.");

			var page = _context.Pages.FirstOrDefault(x => x.Id == id);
			if (page == null)
			{
				throw new ArgumentException("Failed to find the page with the provided ID.", nameof(id));
			}

			if (_settingsService.SoftDelete)
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
			VerifyAccess("You must be authenticate to delete a tag.");

			if (string.IsNullOrWhiteSpace(tag))
			{
				throw new ArgumentException("The tag name must be provided.", nameof(tag));
			}

			var name1 = "," + tag + ",";
			var pagesToUpdate = GetPageQuery().Where(x => x.Tags.Contains(name1)).ToList();
			pagesToUpdate.ForEach(page => page.Tags = page.Tags.Replace(name1, ","));

			_context.SaveChanges();

			pagesToUpdate.ForEach(page => _searchService.Update(page.ToView(_converter)));
		}

		public FileView GetFile(int id, bool includeData = false)
		{
			var file = _context.Files.FirstOrDefault(x => x.Id == id && !x.IsDeleted);
			if (file == null)
			{
				throw new ArgumentException("The file could not be found.", nameof(id));
			}

			return file.ToView(includeData);
		}

		public FileView GetFile(string name, bool includeData = false)
		{
			var file = _context.Files.FirstOrDefault(x => x.Name == name && !x.IsDeleted);
			if (file == null)
			{
				throw new ArgumentException("The file could not be found.", nameof(name));
			}

			return file.ToView(includeData);
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
				.FirstOrDefault();

			return page != null
				? page.ToView(_converter)
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

			return page.ToView(_converter);
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
				ModifiedBy = version.EditedBy.DisplayName
			};

			if (previous == null)
			{
				response.Html = _converter.ToHtml(version.Text);
				response.Title = version.Title;
				response.TitleForLink = PageView.ConvertTitleForLink(version.Title);
			}
			else
			{
				var service = new HtmlDiff(_converter.ToHtml(previous.Text), _converter.ToHtml(version.Text));
				response.Html = service.Build();
				response.Title = previous.Title;
				response.TitleForLink = PageView.ConvertTitleForLink(previous.Title);
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

			return page.ToHistoryView();
		}

		public PagedResults<PageView> GetPages(PagedRequest request = null)
		{
			var query = GetPageQuery(x => x.ModifiedBy);
			request = request ?? new PagedRequest();

			if (!string.IsNullOrWhiteSpace(request.Filter))
			{
				var filter = RequestFilter.Parse(request.Filter);
				query = ProcessFilter(query, filter);
			}

			return GetPagedResults(query, request, x => x.Title, x => x.ToSummaryView());
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

		public UserView GetUser(int id)
		{
			if (_user == null || !_user.InRole("Administrator"))
			{
				throw new UnauthorizedAccessException("You do not have the permission to be access users.");
			}

			return _context.Users.FirstOrDefault(x => x.Id == id)?.ToView();
		}

		public PagedResults<UserView> GetUsers(PagedRequest request = null)
		{
			if (_user == null || !_user.InRole("Administrator"))
			{
				throw new UnauthorizedAccessException("You do not have the permission to be access users.");
			}

			var query = _context.Users.OrderBy(x => x.DisplayName).AsQueryable();
			request = request ?? new PagedRequest();

			if (!string.IsNullOrWhiteSpace(request.Filter))
			{
				var filter = RequestFilter.Parse(request.Filter);
				query = ProcessFilter(query, filter);
			}

			return GetPagedResults(query, request, x => x.UserName, x => x.ToView());
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
			VerifyAccess("You must be authenticate to rename the tag.");

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

			pagesToUpdate.ForEach(page => _searchService.Update(page.ToView(_converter)));
		}

		public int SaveFile(FileView view)
		{
			VerifyAccess("You must be authenticate to save the file.");

			var file = _context.Files.FirstOrDefault(x => x.Name == view.Name)
				?? new File { Name = view.Name, CreatedBy = _user, CreatedOn = DateTime.UtcNow };

			if (!_settingsService.OverwriteFilesOnUpload && file.Id != 0)
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
			VerifyAccess("You must be authenticate to save the page.");

			var page = GetPageQuery().FirstOrDefault(x => x.Id == view.Id)
				?? new Page { CreatedOn = DateTime.UtcNow, CreatedBy = _user };

			if (page.EditingById != null && page.EditingById != _user?.Id)
			{
				throw new InvalidOperationException("This page is currently being edited by another user.");
			}

			var tags = $",{string.Join(",", view.Tags.Select(x => x.Trim()).Distinct().OrderBy(x => x))},";

			// Only create history on changes for Tags, Text, and/or Title.
			if (tags != page.Tags || page.Text != view.Text || page.Title != view.Title)
			{
				// Only create version when editing existing page.
				if (view.Id > 0)
				{
					var version = new PageHistory
					{
						EditedBy = page.ModifiedBy,
						EditedOn = page.ModifiedOn,
						ApprovalStatus = page.ApprovalStatus,
						Tags = page.Tags,
						Text = page.Text,
						Title = page.Title
					};

					page.History.Add(version);
				}

				page.Tags = tags;
				page.Text = view.Text;
				page.Title = view.Title;
				page.ModifiedBy = _user;
				page.ModifiedOn = DateTime.UtcNow;
			}

			page.ApprovalStatus = view.ApprovalStatus == ApprovalStatus.Pending ? ApprovalStatus.Pending : ApprovalStatus.None;
			page.EditingById = null;
			page.EditingOn = SqlDateTime.MinValue.Value;

			_context.Pages.AddOrUpdate(page);
			_context.SaveChanges();

			return page.ToView(_converter);
		}

		public UserView SaveUser(UserView view)
		{
			VerifyAccess("You do not have the permission to be access users.", "administrator");

			var user = _context.Users.FirstOrDefault(x => x.Id == view.Id);
			if (user == null)
			{
				throw new UserNotFoundException("The user could not be found.");
			}

			user.DisplayName = view.DisplayName;
			user.EmailAddress = view.EmailAddress;
			user.UserName = view.UserName;
			user.Tags = $",{string.Join(",", view.Tags.Select(x => x.Trim()).Distinct().OrderBy(x => x))},";

			_context.SaveChanges();

			return user.ToView();
		}

		public PageView UpdatePage(PageUpdate update)
		{
			if ((update.Type.HasFlag(PageUpdateType.Approve) || update.Type.HasFlag(PageUpdateType.Reject)) &&
				_user != null && !_user.InRole("Approver"))
			{
				throw new UnauthorizedAccessException("You do not have the permission to update this page.");
			}

			if ((update.Type == PageUpdateType.Publish || update.Type == PageUpdateType.Unpublish) &&
				_user != null && !_user.InRole("Publisher"))
			{
				throw new UnauthorizedAccessException("You do not have the permission to update this page.");
			}

			var page = GetPageQuery().FirstOrDefault(x => x.Id == update.Id);
			if (page == null)
			{
				throw new PageNotFoundException("Failed to find the page with that ID.");
			}

			if (update.Type.HasFlag(PageUpdateType.Pending))
			{
				page.ApprovalStatus = ApprovalStatus.Pending;
			}
			else if (update.Type.HasFlag(PageUpdateType.Approve))
			{
				page.ApprovalStatus = ApprovalStatus.Approved;
			}
			else if (update.Type.HasFlag(PageUpdateType.Reject))
			{
				page.ApprovalStatus = ApprovalStatus.Rejected;
			}
			else if (update.Type.HasFlag(PageUpdateType.Publish))
			{
				page.IsPublished = true;
			}
			else if (update.Type.HasFlag(PageUpdateType.Unpublish))
			{
				page.IsPublished = false;
			}

			_context.SaveChanges();
			return page.ToView(_converter);
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

			if (_user == null && _settingsService.EnablePageApproval)
			{
				query = query.Where(x => x.IsPublished);
			}

			return query;
		}

		private static IQueryable<User> ProcessFilter(IQueryable<User> query, RequestFilter filter)
		{
			foreach (var item in filter)
			{
				switch (item.Key.ToLower())
				{
					case "tags":
						var tag = $",{item.Value},";
						query = query.Where(x => x.Tags.Contains(tag));
						break;

					default:
						query = query.Where(x => x.UserName.Contains(item.Value) || x.EmailAddress.Contains(item.Value));
						break;
				}
			}

			return query;
		}

		private static IQueryable<Page> ProcessFilter(IQueryable<Page> query, RequestFilter filter)
		{
			foreach (var item in filter)
			{
				switch (item.Key.ToLower())
				{
					case "status":
						var status = (ApprovalStatus) Enum.Parse(typeof (ApprovalStatus), item.Value);
						query = query.Where(x => x.ApprovalStatus == status);
						break;

					case "tags":
						var tag = $",{item.Value},";
						query = query.Where(x => x.Tags.Contains(tag));
						break;

					default:
						query = query.Where(x => x.Title.Contains(item.Value));
						break;
				}
			}

			return query;
		}

		private void UpdateEditingPage(PageView model)
		{
			var page = GetPageQuery().FirstOrDefault(x => x.Id == model.Id);
			if (page == null)
			{
				throw new ArgumentException("The page could not be found.", nameof(model));
			}

			page.EditingOn = DateTime.UtcNow;
		}

		private void VerifyAccess(string message, string role = null)
		{
			if (_user == null || (role != null && !_user.InRole(role)))
			{
				throw new UnauthorizedAccessException(message);
			}
		}

		#endregion
	}
}