#region References

using System;
using System.Linq;
using System.Security.Authentication;
using System.Web.Mvc;
using Scribe.Data;
using Scribe.Extensions;
using Scribe.Models.Entities;
using Scribe.Services;

#endregion

namespace Scribe.Website.Controllers
{
	public class BaseController : Controller
	{
		#region Constructors

		public BaseController(IScribeContext dataContext, IAuthenticationService authenticationService)
		{
			DataContext = dataContext;
			AuthenticationService = authenticationService;
		}

		#endregion

		#region Properties

		public IAuthenticationService AuthenticationService { get; }

		public IScribeContext DataContext { get; }
		
		#endregion

		#region Methods

		/// <summary>
		/// Gets the current logged in user using the provided session.
		/// </summary>
		/// <param name="throwException">
		/// If true then throw an exception if the user is not logged in else return null.
		/// </param>
		/// <returns> The user of the logged in user. </returns>
		public User GetCurrentUser(bool throwException = true)
		{
			// Make sure we are authenticated.
			if (!User.Identity.IsAuthenticated)
			{
				if (throwException)
				{
					throw new Exception(Constants.NotAuthorized);
				}

				return null;
			}

			var userId = User.Identity.GetId();
			var user = DataContext.Users.FirstOrDefault(u => u.Id == userId);
			if (user == null)
			{
				// Log the user out because we cannot find the user account.
				AuthenticationService.LogOut();

				if (throwException)
				{
					throw new AuthenticationException(Constants.LoggedInUserNotFound);
				}
			}

			return user;
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
				DataContext.Dispose();
			}

			base.Dispose(disposing);
		}

		#endregion
	}
}