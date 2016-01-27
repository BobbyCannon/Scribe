#region References

using System.Collections.Generic;

#endregion

namespace Scribe.Models.Views
{
	public class TagPagesView
	{
		#region Properties

		public IEnumerable<PageSummaryView> Pages { get; set; }

		public string Tag { get; set; }

		#endregion
	}
}