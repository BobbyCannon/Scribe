#region References

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlTypes;
using System.Linq;
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
		private readonly SearchService _searchService;
		private readonly SettingsService _settings;
		private readonly User _user;

		#endregion

		#region Constructors

		public ScribeService(IScribeContext context, AccountService accountService, SearchService searchService, User user)
		{
			_context = context;
			_accountService = accountService;
			_converter = new MarkupConverter(_context);
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
			var page = GetPageQuery()
				.Include(x => x.CreatedBy)
				.Include(x => x.ModifiedBy)
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

			response.Files = GetFiles();
			response.Pages = GetPages().Select(x => x.Title).ToList();

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

		public IEnumerable<FileView> GetFiles(string filter = null, bool includeData = false)
		{
			return _context.Files
				.Where(x => !x.IsDeleted)
				.OrderBy(x => x.Name)
				.Select(x => new FileView
				{
					Id = x.Id,
					Name = x.Name,
					Size = x.Size / 1024 + " kb",
					Type = x.Type
				})
				.ToList();
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
			var page = GetPageQuery()
				.Include(x => x.CreatedBy)
				.Include(x => x.ModifiedBy)
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
				.Include(x => x.Page)
				.Include(x => x.EditedBy)
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

		public IEnumerable<PageView> GetPages(string filter = null)
		{
			return GetPageQuery()
				.Include(x => x.ModifiedBy)
				.ToList()
				.Select(x => new PageView
				{
					Id = x.Id,
					LastModified = DateTime.UtcNow.Subtract(x.ModifiedOn).ToTimeAgo(),
					ModifiedBy = x.ModifiedBy.DisplayName,
					ModifiedOn = x.ModifiedOn,
					Title = x.Title,
					TitleForLink = PageView.ConvertTitleForLink(x.Title)
				})
				.OrderBy(x => x.Title);
		}

		public TagPagesView GetPagesWithTag(string tag)
		{
			var formattedTag = "," + tag + ",";

			return new TagPagesView
			{
				Tag = tag,
				Pages = GetPageQuery()
					.Where(x => x.Tags.Contains(formattedTag))
					.Select(x => new { x.Id, x.Title, x.Tags })
					.ToList()
					.Where(x => x.Tags.Contains(formattedTag))
					.Select(x => new PageSummaryView
					{
						Id = x.Id,
						Title = x.Title
					})
					.OrderBy(x => x.Title)
					.ToList()
			};
		}

		public IEnumerable<TagView> GetTags(string filter = null)
		{
			var pagesWithTags = GetPageQuery()
				.Select(x => new { x.Title, x.Tags })
				.ToList();

			return pagesWithTags
				.SelectMany(x => x.Tags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				.GroupBy(x => x)
				.Select(x => TagView.Create(x.Key, x.Count()))
				.OrderBy(x => x.Tag);
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

		public void SaveFile(FileData data)
		{
			var file = _context.Files.FirstOrDefault(x => x.Name == data.Name)
				?? new File { Name = data.Name, CreatedBy = _user, CreatedOn = DateTime.UtcNow };

			if (!_settings.OverwriteFilesOnUpload && file.Id != 0)
			{
				throw new InvalidOperationException("The file already exists and cannot be overwritten.");
			}

			file.Data = data.Data;
			file.Size = data.Data.Length;
			file.Type = data.Type;
			file.ModifiedOn = file.Id == 0 ? file.CreatedOn : DateTime.UtcNow;
			file.ModifiedBy = _user;

			_context.Files.AddOrUpdate(file);
			_context.SaveChanges();
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

		private IQueryable<Page> GetPageQuery()
		{
			var query = _context.Pages.Where(x => !x.IsDeleted);

			if (_user == null && _settings.EnablePublicTag)
			{
				query = query.Where(x => x.Tags.Contains(",public,"));
			}

			return query;
		}

		#endregion
	}
}