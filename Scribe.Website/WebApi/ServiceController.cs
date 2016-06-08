#region References

using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Services;

#endregion

namespace Scribe.Website.WebApi
{
	[ApiExplorerSettings(IgnoreApi = true)]
	public class ServiceController : BaseApiController, IScribeService
	{
		#region Fields

		private ScribeService _service;

		#endregion

		#region Constructors

		public ServiceController(IScribeDatabase dataDatabase, IAuthenticationService authenticationService)
			: base(dataDatabase, authenticationService)
		{
		}

		#endregion

		#region Methods

		/// <summary>Initializes the <see cref="T:System.Web.Http.ApiController" /> instance with the specified controllerContext.</summary>
		/// <param name="controllerContext">The <see cref="T:System.Web.Http.Controllers.HttpControllerContext" /> object that is used for the initialization.</param>
		protected override void Initialize(HttpControllerContext controllerContext)
		{
			var path = HostingEnvironment.MapPath("~/App_Data/Indexes");
			var searchService = new SearchService(DataDatabase, path, GetCurrentUser(controllerContext,false));
			var accountService = new AccountService(DataDatabase, AuthenticationService);
			_service = new ScribeService(DataDatabase, accountService, searchService, GetCurrentUser(controllerContext,false));
			base.Initialize(controllerContext);
		}

		public PageView BeginEditingPage(int id)
		{
			return _service.BeginEditingPage(id);
		}

		[HttpPost]
		public void CancelEditingPage(int id)
		{
			_service.CancelEditingPage(id);
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
			return _service.SavePage(view);
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

		#endregion
	}
}