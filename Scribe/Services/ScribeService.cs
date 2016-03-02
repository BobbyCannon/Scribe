#region References

using System;
using System.Collections.Generic;
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
			_converter.LinkParsed += (title, title2) => GetCurrentPagesQuery().Where(x => x.Title == title || x.Title == title2).Select(x => new PageView { Id = x.Id, Title = x.Title }).FirstOrDefault();
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

		public bool IsGuestRequest => _user == null && _settingsService.EnableGuestMode;

		#endregion

		#region Methods

		public PageView BeginEditingPage(int id)
		{
			VerifyAccess("You must be authenticate to begin editing page.");

			var page = GetCurrentPagesQuery()
				.FirstOrDefault(x => x.ParentId == id);

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

			var page = GetCurrentPagesQuery().FirstOrDefault(x => x.ParentId == id);
			if (page == null)
			{
				throw new ArgumentException("The page could not be found.", nameof(id));
			}

			page.EditingBy = null;
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

			if (_settingsService.SoftDelete)
			{
				_context.Pages.Where(x => x.ParentId == id).ForEach(x => x.IsDeleted = true);
			}
			else
			{
				_context.Pages.RemoveRange(x => x.ParentId == id);
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
			var pagesToUpdate = GetCurrentPagesQuery().Where(x => x.Tags.Contains(name1)).ToList();
			pagesToUpdate.ForEach(page => page.Tags = page.Tags.Replace(name1, ","));

			_context.SaveChanges();

			pagesToUpdate.ForEach(page => _searchService.Add(page.ToView(_converter)));
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

			return GetPagedResults(query, request, x => x.Name, x => new FileView
			{
				Id = x.Id,
				Name = x.Name,
				Size = x.Size / 1024 + " kb",
				Type = x.Type
			});
		}

		public PageView GetFrontPage()
		{
			var page = GetCurrentPagesQuery().FirstOrDefault(x => x.IsHomePage);

			return page != null
				? page.ToView(_converter)
				: new PageView
				{
					Html = "A home page has not been set. Please see an administrator to have one set.",
					Text = "A home page has not been set. Please see an administrator to have one set.",
					Title = "Home Page Not Set"
				};
		}

		public PageView GetPage(int id, bool includeHistory = false)
		{
			var page = GetCurrentPagesQuery()
				.FirstOrDefault(x => x.ParentId == id);

			if (page == null)
			{
				throw new PageNotFoundException("Failed to find the page with that ID.");
			}

			return page.ToView(_converter);
		}

		public PageDifferenceView GetPageDifference(int id)
		{
			var version = GetAllPagesQuery().FirstOrDefault(x => x.Id == id);
			if (version == null)
			{
				throw new PageNotFoundException("Failed to find the page with that version ID.");
			}

			if (IsGuestRequest && (version.ApprovalStatus != ApprovalStatus.Approved || !version.IsPublished))
			{
				throw new PageNotFoundException("Failed to find the page with that version ID.");
			}

			var versions = IsGuestRequest
				? version.Parent.Versions.Where(x => x.IsPublished && x.ApprovalStatus == ApprovalStatus.Approved)
				: version.Parent.Versions;

			var previous = versions
				.Where(x => x.Id < version.Id)
				.OrderByDescending(x => x.Id)
				.FirstOrDefault();

			var response = new PageDifferenceView
			{
				Id = version.ParentId ?? version.Id,
				LastModified = DateTime.UtcNow.Subtract(version.ModifiedOn).ToTimeAgo(),
				CreatedBy = version.CreatedBy.DisplayName
			};

			if (previous == null)
			{
				response.Html = _converter.ToHtml(version.Text);
				response.Title = version.Title;
				response.TitleForLink = PageView.ConvertTitleForLink(version.Title);
				response.Tags = string.Join(", ", Page.SplitTags(version.Tags));
			}
			else
			{
				response.Html = HtmlDiff.Process(_converter.ToHtml(previous.Text), _converter.ToHtml(version.Text));
				response.Title = HtmlDiff.Process(_converter.ToHtml(previous.Title), _converter.ToHtml(version.Title));
				response.TitleForLink = PageView.ConvertTitleForLink(previous.Title);
				response.Tags = HtmlDiff.Process(_converter.ToHtml(string.Join(", ", Page.SplitTags(previous.Tags))), _converter.ToHtml(string.Join(", ", Page.SplitTags(version.Tags))));
			}

			return response;
		}

		public PageHistoryView GetPageHistory(int id)
		{
			var page = GetCurrentPagesQuery().FirstOrDefault(x => x.ParentId == id);
			if (page == null)
			{
				throw new PageNotFoundException("Failed to find the page with that ID.");
			}

			return page.ToHistoryView(IsGuestRequest);
		}

		public PagedResults<PageView> GetPages(PagedRequest request = null)
		{
			var query = GetCurrentPagesQuery();
			request = request ?? new PagedRequest();

			if (!string.IsNullOrWhiteSpace(request.Filter))
			{
				var filter = RequestFilter.Parse(request.Filter);
				query = ProcessFilter(query, filter);
			}

			return GetPagedResults(query.Select(x => new
			{
				x.ApprovalStatus,
				CreatedBy = x.CreatedBy.DisplayName,
				x.CreatedOn,
				EditingBy = x.EditingBy != null ? x.EditingBy.DisplayName : string.Empty,
				Id = x.ParentId ?? x.Id,
				x.IsHomePage,
				x.IsPublished,
				x.Tags,
				x.Text,
				x.Title,
			}), request, x => x.Title, x => new PageView
			{
				ApprovalStatus = x.ApprovalStatus,
				Id = x.Id,
				CreatedBy = x.CreatedBy,
				CreatedOn = x.CreatedOn,
				EditingBy = x.EditingBy,
				Files = new List<FileView>(),
				Html = request.IncludeDetails ? _converter.ToHtml(x.Text) : string.Empty,
				IsHomePage = x.IsHomePage,
				IsPublished = x.IsPublished,
				LastModified = DateTime.UtcNow.Subtract(x.CreatedOn).ToTimeAgo(),
				Pages = new List<string>(),
				Tags = Page.SplitTags(x.Tags),
				Text = request.IncludeDetails ? x.Text : string.Empty,
				Title = x.Title,
				TitleForLink = PageView.ConvertTitleForLink(x.Title)
			});
		}

		public PagedResults<TagView> GetTags(PagedRequest request = null)
		{
			request = request ?? new PagedRequest();

			var query = GetCurrentPagesQuery()
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
			var pagesToUpdate = GetCurrentPagesQuery()
				.Where(x => x.Tags.Contains(name1)).ToList();

			pagesToUpdate.ForEach(page => page.Tags = page.Tags.Replace(name1, name2));

			_context.SaveChanges();

			pagesToUpdate.ForEach(page => _searchService.Add(page.ToView(_converter)));
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

			if (view.Id == 0 && GetCurrentPagesQuery().Any(x => x.Title == view.Title))
			{
				throw new InvalidOperationException("This page title is already used.");
			}

			Page version = null;
			var currentTime = DateTime.UtcNow;
			var tags = $",{string.Join(",", view.Tags.Select(x => x.Trim()).Distinct().OrderBy(x => x))},";

			var page = GetCurrentPagesQuery().FirstOrDefault(x => x.ParentId == view.Id) ?? new Page
			{
				ApprovalStatus = view.ApprovalStatus == ApprovalStatus.Pending ? ApprovalStatus.Pending : ApprovalStatus.None,
				CreatedOn = currentTime,
				CreatedBy = _user,
				ModifiedOn = currentTime,
				EditingById = null,
				EditingOn = SqlDateTime.MinValue.Value,
				IsDeleted = false,
				IsHomePage = false,
				IsPublished = false,
				Tags = tags,
				Text = view.Text,
				Title = view.Title
			};

			if (page.EditingById != null && page.EditingById != _user?.Id)
			{
				throw new InvalidOperationException("This page is currently being edited by another user.");
			}

			// Only add a new version if there was an existing page.
			if (tags != page.Tags || page.Text != view.Text || page.Title != view.Title)
			{
				version = new Page
				{
					ApprovalStatus = view.ApprovalStatus == ApprovalStatus.Pending ? ApprovalStatus.Pending : ApprovalStatus.None,
					CreatedOn = currentTime,
					CreatedBy = _user,
					ModifiedOn = currentTime,
					EditingById = null,
					EditingOn = SqlDateTime.MinValue.Value,
					IsDeleted = false,
					IsHomePage = page.Parent.IsHomePage,
					IsPublished = false,
					Tags = tags,
					Text = view.Text,
					Title = view.Title
				};

				page.Parent.Versions.Add(version);
			}

			page.EditingById = null;
			page.EditingOn = SqlDateTime.MinValue.Value;

			_context.Pages.AddOrUpdate(page);
			_context.SaveChanges();

			if (page.Parent == null && page.ParentId == null)
			{
				page.ParentId = page.Id;
				_context.SaveChanges();
			}

			// Return the new version or the current unchanged version.
			var result = (version ?? page).ToView(_converter);
			_searchService.Add(result);

			return result;
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
				_user != null && !_user.InRole("approver"))
			{
				throw new UnauthorizedAccessException("You do not have the permission to update this page.");
			}

			if ((update.Type.HasFlag(PageUpdateType.Publish) || update.Type.HasFlag(PageUpdateType.Unpublish)) &&
				_user != null && !_user.InRole("publisher"))
			{
				throw new UnauthorizedAccessException("You do not have the permission to update this page.");
			}

			if (update.Type.HasFlag(PageUpdateType.SetHomepage) && _user != null && !_user.InRole("administrator"))
			{
				throw new UnauthorizedAccessException("You do not have the permission to update this page.");
			}

			var page = GetCurrentPagesQuery().FirstOrDefault(x => x.ParentId == update.Id);
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

			if (update.Type.HasFlag(PageUpdateType.Publish))
			{
				page.IsPublished = true;
			}
			else if (update.Type.HasFlag(PageUpdateType.Unpublish))
			{
				page.IsPublished = false;
			}

			if (update.Type.HasFlag(PageUpdateType.SetHomepage))
			{
				_context.Pages.Where(x => x.IsHomePage).ForEach(x => x.IsHomePage = false);
				page.Parent.Versions.ForEach(x => x.IsHomePage = true);
			}

			_context.SaveChanges();
			return page.ToView(_converter);
		}

		private IQueryable<Page> GetAllPagesQuery(params Expression<Func<Page, object>>[] includes)
		{
			var query = includes != null ? _context.Pages.Including(includes) : _context.Pages;
			query = query.Where(x => !x.IsDeleted);

			if (IsGuestRequest)
			{
				query = query.Where(x => x.IsPublished && x.ApprovalStatus == ApprovalStatus.Approved);
			}

			query = query.OrderByDescending(x => x.Id);
			return query;
		}

		private IQueryable<Page> GetCurrentPagesQuery()
		{
			return GetAllPagesQuery()
				.GroupBy(x => x.ParentId)
				.Select(x => x.OrderByDescending(y => y.Id).FirstOrDefault());
		}

		private PagedResults<T2> GetPagedResults<T1, T2>(IQueryable<T1> query, PagedRequest request, Func<T1, object> orderBy, Func<T1, T2> transform)
		{
			var response = new PagedResults<T2>();
			response.Filter = request.Filter;
			response.TotalCount = query.Count();
			response.PerPage = request.PerPage;
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
			var page = GetCurrentPagesQuery().FirstOrDefault(x => x.ParentId == model.Id);
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