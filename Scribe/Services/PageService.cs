#region References

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.SqlTypes;
using System.Linq;
using System.Web;
using Scribe.Converters;
using Scribe.Data;
using Scribe.Exceptions;
using Scribe.Extensions;
using Scribe.Models.Entities;
using Scribe.Models.Views;

#endregion

namespace Scribe.Services
{
	/// <summary>
	/// Represents the service to handle all the page tasks.
	/// </summary>
	public class PageService
	{
		#region Fields

		private readonly IScribeContext _context;
		private readonly MarkupConverter _converter;
		private readonly SettingsService _settings;
		private readonly User _user;

		#endregion

		#region Constructors

		public PageService(IScribeContext context, User user)
		{
			_context = context;
			_converter = new MarkupConverter(_context);
			_settings = new SettingsService(context, user);
			_user = user;
		}

		static PageService()
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
			}

			return new PageView(page, _converter);
		}

		public void CancelEditingPage(int id)
		{
			var page = GetPageQuery().FirstOrDefault(x => x.Id == id);
			if (page == null)
			{
				throw new ArgumentException("The page could not be found.", nameof(id));
			}

			page.EditingById = null;
			page.EditingOn = SqlDateTime.MinValue.Value;
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

		public PageView GetPage(int id)
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

		public IEnumerable<PageSummaryView> GetPages()
		{
			return GetPageQuery()
				.Include(x => x.ModifiedBy)
				.ToList()
				.Select(x => new PageSummaryView
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

		public IEnumerable<TagView> GetTags()
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

		public IEnumerable<Page> RenameTag(string oldName, string newName)
		{
			if (oldName.Equals("public", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException("Cannot rename the public tag.");
			}

			if (newName.Equals("public", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException("Cannot rename the tag to public.");
			}

			if (string.IsNullOrWhiteSpace(oldName))
			{
				throw new ArgumentException("The old name must be provided.", nameof(oldName));
			}

			if (string.IsNullOrWhiteSpace(newName))
			{
				throw new ArgumentException("The new name must be provided.", nameof(newName));
			}

			var name1 = "," + oldName + ",";
			var name2 = "," + newName + ",";
			var pagesToUpdate = GetPageQuery().Where(x => x.Tags.Contains(name1)).ToList();
			foreach (var page in pagesToUpdate.Where(x => x.Tags.Contains(name1)))
			{
				page.Tags = page.Tags.Replace(name1, name2);
			}

			return pagesToUpdate;
		}

		public Page Save(PageView view)
		{
			var page = GetPageQuery().FirstOrDefault(x => x.Id == view.Id)
				?? new Page { CreatedOn = DateTime.UtcNow, CreatedBy = _user, IsLocked = false };

			if (page.EditingById != null && page.EditingById != _user?.Id)
			{
				throw new HttpException(403, "This page is currently being edited by another user.");
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

			return page;
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
			var query = _context.Pages.AsQueryable();
			if (_user == null && _settings.EnablePublicTag)
			{
				query = query.Where(x => x.Tags.Contains(",public,"));
			}

			return query;
		}

		#endregion
	}
}