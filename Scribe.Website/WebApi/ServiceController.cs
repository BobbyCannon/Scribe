#region References

using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Hubs;
using Scribe.Website.Services;

#endregion

namespace Scribe.Website.WebApi
{
	[ApiExplorerSettings(IgnoreApi = true)]
	public class ServiceController : BaseApiController, IScribeService
	{
		#region Fields

		private readonly INotificationHub _notificationHub;
		private ScribeService _service;

		#endregion

		#region Constructors

		public ServiceController(IScribeDatabase database, IAuthenticationService authenticationService, INotificationHub notificationHub)
			: base(database, authenticationService)
		{
			_notificationHub = notificationHub;
		}

		#endregion

		#region Methods

		public PageView BeginEditingPage(int id)
		{
			var view = _service.BeginEditingPage(id);
			Database.SaveChanges();
			_notificationHub.PageLockedForEdit(id, view.EditingBy);
			return view;
		}

		[HttpPost]
		public void CancelEditingPage(int id)
		{
			_service.CancelEditingPage(id);
			_notificationHub.PageAvailableForEdit(id);
		}

		[HttpPost]
		public void DeleteFile(int id)
		{
			_service.DeleteFile(id);
		}

		[HttpPost]
		public void DeletePage(int id)
		{
			_service.DeletePage(id);
		}

		[HttpPost]
		public void DeleteTag(string name)
		{
			_service.DeleteTag(name);
		}

		[AllowAnonymous]
		public FileView GetFile(int id, bool includeData)
		{
			return _service.GetFile(id, includeData);
		}

		[HttpPost]
		[AllowAnonymous]
		public PagedResults<FileView> GetFiles(PagedRequest request)
		{
			return _service.GetFiles(request);
		}

		[AllowAnonymous]
		public PageView GetPage(int id, bool includeHistory)
		{
			return _service.GetPage(id, includeHistory);
		}

		[HttpPost]
		public string GetPagePreview(PageView model)
		{
			return _service.GetPagePreview(model);
		}

		[HttpPost]
		[AllowAnonymous]
		public PagedResults<PageView> GetPages(PagedRequest request)
		{
			return _service.GetPages(request);
		}

		[HttpPost]
		[AllowAnonymous]
		public PagedResults<TagView> GetTags(PagedRequest request)
		{
			return _service.GetTags(request);
		}

		[Authorize(Roles = "Administrator")]
		public UserView GetUser(int id)
		{
			return _service.GetUser(id);
		}

		[HttpPost]
		[Authorize(Roles = "Administrator")]
		public PagedResults<UserView> GetUsers(PagedRequest request)
		{
			return _service.GetUsers(request);
		}

		[HttpPost]
		[AllowAnonymous]
		public void LogIn(Credentials login)
		{
			_service.LogIn(login);
		}

		[HttpPost]
		public void LogOut()
		{
			_service.LogOut();
		}

		[HttpPost]
		public void RenameTag(RenameValues values)
		{
			_service.RenameTag(values);
		}

		[HttpPost]
		public int SaveFile(FileView view)
		{
			return _service.SaveFile(view);
		}

		[HttpPost]
		public PageView SavePage(PageView view)
		{
			var response = _service.SavePage(view);
			_notificationHub.PageAvailableForEdit(response.Id);
			return response;
		}

		[HttpPost]
		[Authorize(Roles = "Administrator")]
		public UserView SaveUser(UserView view)
		{
			return _service.SaveUser(view);
		}

		[HttpPost]
		public PageView UpdatePage(PageUpdate update)
		{
			return _service.UpdatePage(update);
		}

		/// <summary> Initializes the <see cref="T:System.Web.Http.ApiController" /> instance with the specified controllerContext. </summary>
		/// <param name="controllerContext">
		/// The <see cref="T:System.Web.Http.Controllers.HttpControllerContext" /> object that is
		/// used for the initialization.
		/// </param>
		protected override void Initialize(HttpControllerContext controllerContext)
		{
			var path = HostingEnvironment.MapPath("~/App_Data/Indexes");
			var searchService = new SearchService(Database, path, GetCurrentUser(controllerContext, false));
			var accountService = new AccountService(Database, AuthenticationService);
			_service = new ScribeService(Database, accountService, searchService, GetCurrentUser(controllerContext, false));
			base.Initialize(controllerContext);
		}

		#endregion
	}
}