#region References

using Scribe.Models.Entities;

#endregion

namespace Scribe.Models.Views
{
	public class ProfileView
	{
		#region Properties

		public bool Disabled { get; set; }
		public string DisplayName { get; set; }
		public string EmailAddress { get; set; }
		public int UserId { get; set; }
		public string UserName { get; set; }

		#endregion

		#region Methods

		public static ProfileView Create(User user)
		{
			return new ProfileView
			{
				Disabled = !user.IsEnabled || user.IsActiveDirectory,
				DisplayName = user.DisplayName,
				EmailAddress = user.EmailAddress,
				UserId = user.Id,
				UserName = user.UserName
			};
		}

		#endregion
	}
}