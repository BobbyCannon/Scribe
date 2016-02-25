namespace Scribe.Models.Views
{
	public class SettingsView
	{
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