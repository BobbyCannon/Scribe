#region References

using System;
using Scribe.Data.Entities;
using Speedy;

#endregion

namespace Scribe.Website.Services.Settings
{
	public class UserSettings : SettingsService
	{
		#region Constructors

		public UserSettings(IRepository<Setting> settings, User user)
			: base(settings, user)
		{
		}

		#endregion

		#region Properties

		public DateTime EmailAddressValidationExpiresOn { get; set; }

		public Guid EmailAddressValidationId { get; set; }

		public DateTime ResetPasswordExpiresOn { get; set; }

		public Guid ResetPasswordId { get; set; }

		#endregion

		#region Methods

		public static UserSettings Load(IRepository<Setting> settings, User user)
		{
			var response = new UserSettings(settings, user);
			response.Load();
			return response;
		}

		#endregion
	}
}