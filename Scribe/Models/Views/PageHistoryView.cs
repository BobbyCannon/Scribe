#region References

using System.Collections.Generic;

#endregion

namespace Scribe.Models.Views
{
	public class PageHistoryView
	{
		#region Properties

		public int Id { get; set; }

		public string Title { get; set; }

		public string TitleForLink { get; set; }

		public IEnumerable<PageHistorySummaryView> Versions { get; set; }

		#endregion
	}
}