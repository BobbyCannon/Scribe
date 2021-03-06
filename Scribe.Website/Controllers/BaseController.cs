﻿#region References

using System;
using System.Linq;
using System.Security.Authentication;
using System.Web.Mvc;
using System.Web.Routing;
using Scribe.Data;
using Scribe.Data.Entities;
using Scribe.Website.Services;

#endregion

namespace Scribe.Website.Controllers
{
	public class BaseController : Controller
	{
		#region Fields

		private User _user;

		#endregion

		#region Constructors

		public BaseController(IScribeDatabase database, IAuthenticationService authenticationService)
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
		/// Gets the current logged in user using the provided session.
		/// </summary>
		/// <param name="requestContext"> </param>
		/// <param name="throwException">
		/// If true then throw an exception if the user is not logged in else return null.
		/// </param>
		/// <returns> The user of the logged in user. </returns>
		public User GetCurrentUser(RequestContext requestContext = null, bool throwException = true)
		{
			if (_user != null)
			{
				return _user;
			}

			// Make sure we are authenticated.
			var identity = (requestContext?.HttpContext?.User ?? User)?.Identity;
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

		protected string GetHostUri()
		{
			return HttpContext.Request.Url?.GetDomain().ToLower() ?? string.Empty;
		}

		#endregion
	}
}