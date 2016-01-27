#region References

using System;

#endregion

namespace Scribe.Models.Views
{
	public class PageSummaryView
	{
		#region Properties

		public int Id { get; set; }

		public string LastModified { get; set; }

		public string ModifiedBy { get; set; }

		public DateTime ModifiedOn { get; set; }

		public string Title { get; set; }

		public string TitleForLink { get; set; }

		#endregion
	}
}