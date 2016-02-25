#region References

using System.Web.Mvc;

#endregion

namespace Scribe.Website.Attributes
{
	public class MvcAuthorizeAttribute : AuthorizeAttribute
	{
		#region Methods

		protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
		{
			if (filterContext.HttpContext.User.Identity.IsAuthenticated)
			{
				filterContext.Result = new RedirectResult("/Unauthorized");
				return;
			}

			base.HandleUnauthorizedRequest(filterContext);
		}

		#endregion
	}
}