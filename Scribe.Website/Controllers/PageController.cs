#region References

using System.Linq;
using System.Reflection;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;
using Bloodhound.Models;
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
		private SearchService _searchService;
		private ScribeService _service;

		#endregion

		#region Constructors

		public PageController(IScribeDatabase database, IAuthenticationService authenticationService, INotificationHub notificationHub)
			: base(database, authenticationService)
		{
			_notificationHub = notificationHub;
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
			Database.SaveChanges();
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

		[AllowAnonymous]
		public ActionResult Manual()
		{
			ViewBag.Version = Assembly.GetExecutingAssembly().GetName().Version;
			return View();
		}

		public ActionResult New(string suggestedTitle)
		{
			return View("Edit", new PageView
			{
				Title = suggestedTitle,
				Files = _service.GetFiles(new PagedRequest { PerPage = int.MaxValue }).Results,
				Pages = _service.GetPages(new PagedRequest { PerPage = int.MaxValue }).Results.Select(x => new PageReference { Id = x.Id, Title = x.Title, TitleForLink = x.TitleForLink }).ToList()
			});
		}

		[AllowAnonymous]
		public ActionResult Page(int id)
		{
			var page = _service.GetPage(id);
			(HttpContext.Items["Event"] as Event)?.Values.Add(new EventValue("Page Id", page.Id));
			(HttpContext.Items["Event"] as Event)?.Values.Add(new EventValue("Page Title", page.Title));
			return View(page);
		}

		[AllowAnonymous]
		public ActionResult Pages()
		{
			var pages = _service.GetPages(new PagedRequest { PerPage = int.MaxValue });
			return View(pages);
		}

		[AllowAnonymous]
		public ActionResult PagesWithTag(string tag)
		{
			return View(_service.GetPages(new PagedRequest { Filter = $"Tags.Contains(\"{tag}\")", Page = 1, PerPage = int.MaxValue }));
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
			var user = GetCurrentUser();
			var service = new SettingsService(Database, user);
			var privateService = new ScribeService(Database, null, null, user);
			var publicService = new ScribeService(Database, null, null, null);

			ViewBag.PrivatePages = privateService.GetPages(new PagedRequest { PerPage = int.MaxValue }).Results.Select(x => new PageReferenceView { Id = x.Id, Title = x.Id + " - " + x.Title });
			ViewBag.PublicPages = publicService.GetPages(new PagedRequest { PerPage = int.MaxValue }).Results.Select(x => new PageReferenceView { Id = x.Id, Title = x.Id + " - " + x.Title });

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
			return View(_service.GetTags(new PagedRequest { PerPage = int.MaxValue }));
		}

		/// <summary> Initializes data that might not be available when the constructor is called. </summary>
		/// <param name="requestContext"> The HTTP context and route data. </param>
		protected override void Initialize(RequestContext requestContext)
		{
			var path = HostingEnvironment.MapPath("~/App_Data/Indexes");
			_searchService = new SearchService(Database, path, GetCurrentUser(requestContext, false));
			var accountService = new AccountService(Database, AuthenticationService);
			_service = new ScribeService(Database, accountService, _searchService, GetCurrentUser(requestContext, false));
			base.Initialize(requestContext);
		}

		#endregion
	}
}