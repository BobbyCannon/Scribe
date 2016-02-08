#region References

using System.Collections.Generic;
using System.Web.Hosting;
using System.Web.Http;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Services;

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

		public IEnumerable<FileView> GetFiles(string filter = null, bool includeData = false)
		{
			return _service.GetFiles(filter, includeData);
		}

		public PageView GetPage(int id, bool includeHistory = false)
		{
			return _service.GetPage(id, includeHistory);
		}

		public IEnumerable<PageView> GetPages(string filter = null)
		{
			return _service.GetPages(filter);
		}

		public IEnumerable<TagView> GetTags(string filter = null)
		{
			return _service.GetTags(filter);
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
		public void SaveFile(FileData data)
		{
			_service.SaveFile(data);
		}

		[HttpPost]
		public PageView SavePage(PageView view)
		{
			return _service.SavePage(view);
		}

		#endregion
	}
}