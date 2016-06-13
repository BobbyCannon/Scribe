#region References

using System;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;
using Scribe.Data;
using Scribe.Data.Entities;
using Scribe.Services;
using Scribe.Website.Services;

#endregion

namespace Scribe.Website.WebApi
{
	public class BaseApiController : ApiController
	{
		#region Fields

		private User _user;

		#endregion

		#region Constructors

		public BaseApiController(IScribeDatabase database, IAuthenticationService authenticationService)
		{
			Database = database;
			AuthenticationService = authenticationService;
		}

		#endregion

		#region Properties

		public IAuthenticationService AuthenticationService { get; }

		public IScribeDatabase Database { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Releases unmanaged resources and optionally releases managed resources.
		/// </summary>
		/// <param name="disposing">
		/// true to release both managed and unmanaged resources; false to release only unmanaged
		/// resources.
		/// </param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Database.Dispose();
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// Gets the current logged in user using the provided session.
		/// </summary>
		/// <param name="database"> </param>
		/// <param name="throwException">
		/// If true then throw an exception if the user is not logged in else return null.
		/// </param>
		/// <returns> The user of the logged in user. </returns>
		protected User GetCurrentUser(HttpControllerContext database = null, bool throwException = true)
		{
			if (_user != null)
			{
				return _user;
			}

			// Make sure we are authenticated.
			var identity = (database?.RequestContext.Principal ?? User)?.Identity;
			if (identity?.IsAuthenticated != true)
			{
				if (throwException)
				{
					throw new Exception(Constants.NotAuthorized);
				}

				return null;
			}

			var userId = identity.GetId();
			_user = Database.Users.FirstOrDefault(u => u.Id == userId);
			if (_user == null)
			{
				// Log the user out because we cannot find the user account.
				AuthenticationService.LogOut();

				if (throwException)
				{
					throw new AuthenticationException(Constants.LoggedInUserNotFound);
				}
			}

			return _user;
		}

		#endregion
	}
}