namespace Scribe.Models.Views
{
	public class SettingsView
	{
		#region Properties

		public bool EnablePublicTag { get; set; }

		public string LdapConnectionString { get; set; }

		public bool OverwriteFilesOnUpload { get; set; }

		#endregion
	}
}