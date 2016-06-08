#region References

using System.Web.Http;
using System.Web.Http.Description;
using Scribe.Models.Data;
using Scribe.Services;
using Scribe.Website.Models;
using Scribe.Website.Services;

#endregion

namespace Scribe.Website.WebApi
{
	[AllowAnonymous]
	[ApiExplorerSettings(IgnoreApi = true)]
	public class DiscoveryController : ApiController
	{
		#region Methods

		[HttpGet]
		[Route("api/Discovery")]
		public PostmanCollection GetDiscovery()
		{
			var service = new DocumentationService();
			return service.GetDiscovery("Scribe API", Request.RequestUri);
		}

		#endregion
	}
}