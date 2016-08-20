#region References

using System.Web.Mvc;
using Scribe.Data;
using Scribe.Website.Services;

#endregion

namespace Scribe.Website.Controllers
{
	[AllowAnonymous]
	public class ErrorController : BaseController
	{
		#region Constructors

		public ErrorController(IScribeDatabase database, IAuthenticationService authenticationService)
			: base(database, authenticationService)
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