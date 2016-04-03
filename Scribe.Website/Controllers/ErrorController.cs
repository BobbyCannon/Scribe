#region References

using System.Web.Mvc;
using Scribe.Data;
using Scribe.Services;

#endregion

namespace Scribe.Website.Controllers
{
	[AllowAnonymous]
	public class ErrorController : BaseController
	{
		#region Constructors

		public ErrorController(IScribeDatabase dataDatabase, IAuthenticationService authenticationService)
			: base(dataDatabase, authenticationService)
		{
		}

		#endregion

		#region Methods

		[ActionName("404")]
		public ActionResult Error404()
		{
			return View("Error404");
		}

		public ActionResult Home()
		{
			return View("Error");
		}

		#endregion
	}
}