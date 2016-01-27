#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;
using System.Web.Security;
using Scribe.Extensions;
using Scribe.Models.Entities;

#endregion

namespace Scribe.Services
{
	[ExcludeFromCodeCoverage]
	public class AuthenticationService : IAuthenticationService
	{
		#region Properties

		/// <summary>
		/// Gets the authenticated state of the user.
		/// </summary>
		public bool IsAuthenticated => UserId != 0;

		/// <summary>
		/// Get the ID of the current authenticated user.
		/// </summary>
		public int UserId => HttpContext.Current.User.Identity.GetId();

		/// <summary>
		/// Gets the user name of the current authenticated user.
		/// </summary>
		public string UserName => HttpContext.Current.User.Identity.GetDisplayName();

		#endregion

		#region Methods

		/// <summary>
		/// Log in the authentication service.
		/// </summary>
		/// <param name="user"> The user to authenticate. </param>
		/// <param name="rememberMe"> The flag to remember the user across sessions. </param>
		/// <returns> </returns>
		public void LogIn(User user, bool rememberMe)
		{
			if (user == null)
			{
				throw new ArgumentException("user");
			}

			HttpContext.Current.Response.Cookies.Clear();
			var displayName = user.DisplayName;
			if (string.IsNullOrEmpty(displayName))
			{
				displayName = user.UserName;
			}

			FormsAuthentication.SetAuthCookie(user.Id + ";" + displayName, rememberMe);
		}

		/// <summary>
		/// Log out of the authentication service.
		/// </summary>
		public void LogOut()
		{
			FormsAuthentication.SignOut();
			ExpireCookies(HttpContext.Current);
		}

		/// <summary>
		/// Update the current authenticated user.
		/// </summary>
		/// <param name="user"> </param>
		public void UpdateLogin(User user)
		{
			var rememberMe = false;
			var authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
			if (authCookie != null)
			{
				var ticket = FormsAuthentication.Decrypt(authCookie.Value);
				rememberMe = ticket != null && ticket.IsPersistent;
			}

			LogIn(user, rememberMe);
		}

		private static void ExpireCookies(HttpContext context)
		{
			var keys = context.Request.Cookies.Cast<string>().ToList();
			foreach (var name in keys)
			{
				var cookie = new HttpCookie(name);
				cookie.Expires = DateTime.UtcNow.AddDays(-1);
				context.Response.Cookies.Add(cookie);
			}
		}

		#endregion
	}
}