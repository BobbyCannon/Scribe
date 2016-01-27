#region References

using System.Linq;
using System.Reflection;
using System.Web.Hosting;
using System.Web.Mvc;
using Scribe.Data;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Hubs;
using WebGrease.Css.Extensions;

#endregion

namespace Scribe.Website.Controllers
{
	public class PageController : BaseController
	{
		#region Fields

		private readonly INotificationHub _notificationHub;
		private readonly SearchService _searchService;

		#endregion

		#region Constructors

		public PageController(IScribeContext dataContext, IAuthenticationService authenticationService, INotificationHub notificationHub)
			: base(dataContext, authenticationService)
		{
			_notificationHub = notificationHub;
			_searchService = new SearchService(dataContext, HostingEnvironment.MapPath("~/App_Data/Indexes"));
		}

		#endregion

		#region Methods

		[AllowAnonymous]
		public ActionResult About()
		{
			ViewBag.Version = Assembly.GetExecutingAssembly().GetName().Version;
			return View();
		}

		public ActionResult CancelEdit(int id)
		{
			var service = new PageService(DataContext, GetCurrentUser());
			service.CancelEditingPage(id);
			DataContext.SaveChanges();
			_notificationHub.PageAvailableForEdit(id);
			return RedirectToAction("Page", new { id });
		}

		[AllowAnonymous]
		public ActionResult Difference(int id)
		{
			var service = new PageService(DataContext);
			return View(service.GetPageDifference(id));
		}

		public ActionResult Edit(int id)
		{
			var service = new PageService(DataContext, GetCurrentUser());
			var view = service.BeginEditingPage(id);
			DataContext.SaveChanges();
			_notificationHub.PageLockedForEdit(id, view.EditingBy);
			view.Files = new FileService(DataContext).GetFiles().Files;
			view.Pages = new PageService(DataContext).GetPages().Select(x => x.Title).ToList();
			return View(view);
		}

		[AllowAnonymous]
		public ActionResult History(int id)
		{
			var service = new PageService(DataContext);
			return View(service.GetPageHistory(id));
		}

		[AllowAnonymous]
		public ActionResult Home()
		{
			if (!MvcApplication.IsConfigured)
			{
				return RedirectToAction("Setup");
			}

			var service = new PageService(DataContext);
			return View("Page", service.GetFrontPage());
		}

		public ActionResult New(string suggestedTitle)
		{
			var view = new FileService(DataContext).GetFiles();
			var pages = new PageService(DataContext).GetPages().Select(x => x.Title).ToList();
			return View("Edit", new PageView { Title = suggestedTitle, Files = view.Files, Pages = pages });
		}

		[AllowAnonymous]
		public ActionResult Page(int id)
		{
			var service = new PageService(DataContext);
			return View(service.GetPage(id));
		}

		[AllowAnonymous]
		public ActionResult Pages()
		{
			var service = new PageService(DataContext);
			return View(service.GetPages());
		}

		[AllowAnonymous]
		public ActionResult PagesWithTag(string tag)
		{
			var service = new PageService(DataContext);
			return View(service.GetPagesWithTag(tag));
		}

		public ActionResult RenameTag(string oldName, string newName)
		{
			var service = new PageService(DataContext);
			var pagesUpdated = service.RenameTag(oldName, newName);
			DataContext.SaveChanges();
			pagesUpdated.ForEach(x => _searchService.Update(x));
			return RedirectToAction("Tags");
		}

		[AllowAnonymous]
		public ActionResult Search(string term)
		{
			return View(_searchService.Search(term));
		}

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
			var service = new PageService(DataContext);
			return View(service.GetTags());
		}

		#endregion
	}
}