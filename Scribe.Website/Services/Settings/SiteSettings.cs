#region References

using Scribe.Data;
using Scribe.Models.Views;

#endregion

namespace Scribe.Website.Services.Settings
{
	public class SiteSettings : SettingsService
	{
		#region Constructors

		private SiteSettings(IScribeDatabase database)
			: base(database.Settings, null)
		{
		}

		#endregion

		#region Properties

		public string ContactEmail { get; set; }

		public bool EnableGuestMode { get; set; }

		public int FrontPagePrivateId { get; set; }

		public int FrontPagePublicId { get; set; }

		public string LdapConnectionString { get; set; }

		public string MailServer { get; set; }

		public bool OverwriteFilesOnUpload { get; set; }

		public string PrintCss { get; set; }

		public bool SoftDelete { get; set; }

		public string ViewCss { get; set; }

		#endregion

		#region Methods

		public void Apply(SettingsView settings)
		{
			ContactEmail = settings.ContactEmail ?? string.Empty;
			EnableGuestMode = settings.EnableGuestMode;
			FrontPagePrivateId = settings.FrontPagePrivateId;
			FrontPagePublicId = settings.FrontPagePublicId;
			LdapConnectionString = settings.LdapConnectionString ?? string.Empty;
			MailServer = settings.MailServer ?? string.Empty;
			OverwriteFilesOnUpload = settings.OverwriteFilesOnUpload;
			PrintCss = settings.PrintCss ?? string.Empty;
			SoftDelete = settings.SoftDelete;
			ViewCss = settings.ViewCss ?? string.Empty;
		}

		public static SiteSettings Load(IScribeDatabase database, bool ignoreCache = false)
		{
			var response = new SiteSettings(database);
			response.Load(ignoreCache);
			return response;
		}

		public SettingsView ToView()
		{
			return new SettingsView
			{
				ContactEmail = ContactEmail,
				EnableGuestMode = EnableGuestMode,
				FrontPagePrivateId = FrontPagePrivateId,
				FrontPagePublicId = FrontPagePublicId,
				LdapConnectionString = LdapConnectionString,
				MailServer = MailServer,
				OverwriteFilesOnUpload = OverwriteFilesOnUpload,
				PrintCss = PrintCss,
				SoftDelete = SoftDelete,
				ViewCss = ViewCss
			};
		}

		#endregion
	}
}