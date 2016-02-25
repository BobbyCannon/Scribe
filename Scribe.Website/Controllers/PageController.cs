﻿#region References

using System.Linq;
using System.Reflection;
using System.Web.Hosting;
using System.Web.Mvc;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Attributes;
using Scribe.Website.Hubs;
using Scribe.Website.Services;

#endregion

namespace Scribe.Website.Controllers
{
	public class PageController : BaseController
	{
		#region Fields

		private readonly INotificationHub _notificationHub;
		private readonly SearchService _searchService;
		private readonly ScribeService _service;

		#endregion

		#region Constructors

		public PageController(IScribeContext dataContext, IAuthenticationService authenticationService, INotificationHub notificationHub)
			: base(dataContext, authenticationService)
		{
			_notificationHub = notificationHub;
			SearchService.SearchPath = HostingEnvironment.MapPath("~/App_Data/Indexes");
			_searchService = new SearchService(DataContext, SearchService.SearchPath, GetCurrentUser(false));
			var accountService = new AccountService(dataContext, authenticationService);
			_service = new ScribeService(DataContext, accountService, _searchService, GetCurrentUser(false));
		}

		#endregion

		#region Methods

		[AllowAnonymous]
		public ActionResult About()
		{
			ViewBag.Version = Assembly.GetExecutingAssembly().GetName().Version;
			return View();
		}

		[AllowAnonymous]
		public ActionResult Difference(int id)
		{
			return View(_service.GetPageDifference(id));
		}

		public ActionResult Edit(int id)
		{
			var view = _service.BeginEditingPage(id);
			DataContext.SaveChanges();
			_notificationHub.PageLockedForEdit(id, view.EditingBy);
			return View(view);
		}

		[AllowAnonymous]
		public ActionResult History(int id)
		{
			return View(_service.GetPageHistory(id));
		}

		[AllowAnonymous]
		public ActionResult Home()
		{
			if (!MvcApplication.IsConfigured)
			{
				return RedirectToAction("Setup");
			}

			return View("Page", _service.GetFrontPage());
		}

		public ActionResult New(string suggestedTitle)
		{
			return View("Edit", new PageView
			{
				Title = suggestedTitle,
				Files = _service.GetFiles(new PagedRequest(perPage: int.MaxValue)).Results,
				Pages = _service.GetPages(new PagedRequest(perPage: int.MaxValue)).Results.Select(x => x.Title).ToList()
			});
		}

		[AllowAnonymous]
		public ActionResult Page(int id)
		{
			return View(_service.GetPage(id));
		}

		[AllowAnonymous]
		public ActionResult Pages()
		{
			return View(_service.GetPages(new PagedRequest(perPage: int.MaxValue)));
		}

		[AllowAnonymous]
		public ActionResult PagesWithTag(string tag)
		{
			return View(_service.GetPages(new PagedRequest($"Tags={tag}", 1, int.MaxValue)));
		}

		public ActionResult RenameTag(string oldName, string newName)
		{
			_service.RenameTag(new RenameValues { NewName = newName, OldName = oldName });
			return RedirectToAction("Tags");
		}

		[AllowAnonymous]
		public ActionResult Search(string term)
		{
			return View(_searchService.Search(term));
		}

		[MvcAuthorize(Roles = "Administrator")]
		public ActionResult Settings()
		{
			var service = new SettingsService(DataContext, GetCurrentUser());
			return View(service.GetSettings());
		}

		[AllowAnonymous]
		public ActionResult Setup()
		{
			return View();
		}

		[AllowAnonymous]
		public ActionResult Tags()
		{
			return View(_service.GetTags(new PagedRequest(perPage: int.MaxValue)));
		}

		#endregion
	}
}