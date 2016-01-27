#region References

using Scribe.Models.Entities;

#endregion

namespace Scribe.Services
{
	public interface IAuthenticationService
	{
		#region Properties

		/// <summary>
		/// Gets the authenticated state of the user.
		/// </summary>
		bool IsAuthenticated { get; }

		/// <summary>
		/// Get the ID of the current authenticated user.
		/// </summary>
		int UserId { get; }

		/// <summary>
		/// Gets the user name of the current authenticated user.
		/// </summary>
		string UserName { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Log in the authentication service.
		/// </summary>
		/// <param name="user"> The user to authenticate. </param>
		/// <param name="rememberMe"> The flag to remember the user across sessions. </param>
		/// <returns> </returns>
		void LogIn(User user, bool rememberMe);

		/// <summary>
		/// Log out of the authentication service.
		/// </summary>
		void LogOut();

		/// <summary>
		/// Update the current authenticated user.
		/// </summary>
		/// <param name="user"> </param>
		void UpdateLogin(User user);

		#endregion
	}
}