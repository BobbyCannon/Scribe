#region References

using System.Web.Mvc;
using Scribe.Website.Attributes;

#endregion

namespace Scribe.Website
{
	public static class FilterConfig
	{
		#region Methods

		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
			filters.Add(new AuthorizeAttribute());
			filters.Add(new MvcExceptionFilterAttribute());
		}

		#endregion
	}
}