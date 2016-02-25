using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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