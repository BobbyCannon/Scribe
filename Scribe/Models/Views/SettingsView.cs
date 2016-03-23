#region References

using System.Collections.Generic;

#endregion

namespace Scribe.Models.Views
{
	public class SettingsView
	{
		#region Constructors

		public SettingsView()
		{
			EnableGuestMode = false;
			FrontPagePrivateId = -1;
			FrontPagePublicId = -1;
			LdapConnectionString = string.Empty;
			OverwriteFilesOnUpload = false;
			PrintCss = string.Empty;
			SoftDelete = true;
			ViewCss = string.Empty;
		}

		#endregion

		#region Properties

		public bool EnableGuestMode { get; set; }

		public int FrontPagePrivateId { get; set; }

		public int FrontPagePublicId { get; set; }

		public string LdapConnectionString { get; set; }

		public bool OverwriteFilesOnUpload { get; set; }

		public string PrintCss { get; set; }

		public IEnumerable<PageReferenceView> PrivatePages { get; set; }

		public IEnumerable<PageReferenceView> PublicPages { get; set; }

		public bool SoftDelete { get; set; }

		public string ViewCss { get; set; }

		#endregion
	}
}