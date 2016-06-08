#region References

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Services;

#endregion

namespace Scribe.Website.Services
{
	public class AuthorizationHeaderHandler : DelegatingHandler
	{
		#region Methods

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			IEnumerable<string> credentials;

			if (!request.Headers.TryGetValues("Authorization", out credentials))
			{
				return base.SendAsync(request, cancellationToken);
			}

			var credentialValues = credentials.First().Replace("Basic ", "").FromBase64().Split(':');
			if (credentialValues.Length != 2)
			{
				return base.SendAsync(request, cancellationToken);
			}

			var database = request.GetDependencyScope().GetService(typeof(IScribeDatabase)) as IScribeDatabase;
			var service = new AccountService(database, null);
			var user = service.Authenticate(new Credentials { UserName = credentialValues[0], Password = credentialValues[1] });
			if (user == null)
			{
				return base.SendAsync(request, cancellationToken);
			}

			var username = user.Id + ";" + user.UserName;
			var usernameClaim = new Claim(ClaimTypes.Name, username);
			var identity = new ClaimsIdentity(new[] { usernameClaim }, "ApiKey");
			var principal = new ClaimsPrincipal(identity);

			HttpContext.Current.User = principal;
			return base.SendAsync(request, cancellationToken);
		}

		#endregion
	}
}