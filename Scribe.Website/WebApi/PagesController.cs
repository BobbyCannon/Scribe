#region References

using System.Collections.Generic;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Controllers;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Services;

#endregion

namespace Scribe.Website.WebApi
{
	[AllowAnonymous]
	public class PagesController : BaseApiController
	{
		#region Fields

		private ScribeService _service;

		#endregion

		#region Constructors

		public PagesController(IScribeDatabase database, IAuthenticationService authenticationService)
			: base(database, authenticationService)
		{
		}

		#endregion

		#region Methods

		[HttpGet]
		[Route("api/Pages")]
		public PagedResults<PageView> Get([FromUri] PagedRequest request)
		{
			return _service.GetPages(request);
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