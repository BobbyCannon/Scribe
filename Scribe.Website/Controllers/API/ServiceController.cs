#region References

using System.Web.Hosting;
using System.Web.Http;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Services;

#endregion

namespace Scribe.Website.Controllers.API
{
	public class ServiceController : BaseApiController, IScribeService
	{
		#region Fields

		private readonly ScribeService _service;

		#endregion

		#region Constructors

		public ServiceController(IScribeContext dataContext, IAuthenticationService authenticationService)
			: base(dataContext, authenticationService)
		{
			SearchService.SearchPath = HostingEnvironment.MapPath("~/App_Data/Indexes");
			var searchService = new SearchService(DataContext, SearchService.SearchPath, GetCurrentUser(false));
			var accountService = new AccountService(dataContext, authenticationService);
			_service = new ScribeService(dataContext, accountService, searchService, GetCurrentUser(false));
		}

		#endregion

		#region Methods

		[HttpPost]
		public void CancelPage(int id)
		{
			_service.CancelPage(id);
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

		public FileView GetFile(int id, bool includeData = false)
		{
			return _service.GetFile(id, includeData);
		}

		public FileView GetFile(string name, bool includeData = false)
		{
			return _service.GetFile(name, includeData);
		}

		[HttpPost]
		public PagedResults<FileView> GetFiles(PagedRequest request = null)
		{
			return _service.GetFiles(request);
		}

		public PageView GetPage(int id, bool includeHistory = false)
		{
			return _service.GetPage(id, includeHistory);
		}

		[HttpPost]
		public PagedResults<PageView> GetPages(PagedRequest request = null)
		{
			return _service.GetPages(request);
		}

		[HttpPost]
		public PagedResults<PageView> GetPagesWithTag(PagedRequest request = null)
		{
			return _service.GetPagesWithTag(request);
		}

		[HttpPost]
		public PagedResults<TagView> GetTags(PagedRequest request = null)
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
		public PagedResults<UserView> GetUsers(PagedRequest request = null)
		{
			return _service.GetUsers(request);
		}

		[HttpPost]
		[Authorize(Roles = "Administrator")]
		public PagedResults<UserView> GetUsersWithTag(PagedRequest request = null)
		{
			return _service.GetUsersWithTag(request);
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
		public string Preview(PageView model)
		{
			return _service.Preview(model);
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

		#endregion
	}
}