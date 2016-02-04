#region References

using System.Web.Hosting;
using System.Web.Http;
using Scribe.Converters;
using Scribe.Data;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Hubs;

#endregion

namespace Scribe.Website.Controllers.API
{
	public class PageController : BaseApiController
	{
		#region Fields

		private readonly INotificationHub _notificationHub;
		
		#endregion

		#region Constructors

		public PageController(IScribeContext dataContext, IAuthenticationService authenticationService, INotificationHub notificationHub)
			: base(dataContext, authenticationService)
		{
			_notificationHub = notificationHub;
		}

		#endregion

		#region Methods

		[HttpPost]
		public string Preview(PageView model)
		{
			if (model.Id > 0)
			{
				var service = new PageService(DataContext, GetCurrentUser());
				service.UpdateEditingPage(model);
				DataContext.SaveChanges();
			}

			var converter = new MarkupConverter(DataContext);
			return converter.ToHtml(model.Text);
		}

		[HttpPost]
		public PageView Save(PageView page)
		{
			var service = new PageService(DataContext, GetCurrentUser());
			var response = service.Save(page);
			if (response == null)
			{
				return page;
			}

			DataContext.SaveChanges();

			var searchService = new SearchService(DataContext, SearchService.SearchPath, GetCurrentUser(false));
			searchService.Update(response);

			_notificationHub.PageAvailableForEdit(response.Id);
			_notificationHub.PageUpdated(response.Id);

			return new PageView(response, new MarkupConverter(DataContext));
		}

		#endregion
	}
}