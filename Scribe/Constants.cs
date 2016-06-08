using System;

namespace Scribe
{
	public static class Constants
	{
		#region Constants

		public const string LoggedInUserNotFound = "Failed to find the logged in user.";
		public const string LoginInvalidError = "The user name or password provided is incorrect.";
		public const string NotAuthorized = "You do not have sufficient privileges for this operation.";

		public static TimeSpan EditingTimeout { get; }

		static Constants()
		{
			EditingTimeout = TimeSpan.FromMinutes(15);
		}

		#endregion
	}
}