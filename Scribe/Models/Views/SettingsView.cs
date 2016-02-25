namespace Scribe.Models.Views
{
	public class SettingsView
	{
		#region Constructors

		public SettingsView()
		{
			EnablePageApproval = false;
			LdapConnectionString = string.Empty;
			OverwriteFilesOnUpload = false;
			PrintCss = string.Empty;
			SoftDelete = true;
			ViewCss = string.Empty;
		}

		#endregion

		#region Properties

		public bool EnablePageApproval { get; set; }

		public string LdapConnectionString { get; set; }

		public bool OverwriteFilesOnUpload { get; set; }

		public string PrintCss { get; set; }

		public bool SoftDelete { get; set; }

		public string ViewCss { get; set; }

		#endregion
	}
}