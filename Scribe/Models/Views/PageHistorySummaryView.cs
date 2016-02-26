#region References

using Scribe.Models.Enumerations;

#endregion

namespace Scribe.Models.Views
{
	public class PageHistorySummaryView
	{
		#region Properties

		public ApprovalStatus ApprovalStatus { get; set; }

		public int Id { get; set; }

		public string LastModified { get; set; }

		public string ModifiedBy { get; set; }

		public int Number { get; set; }

		#endregion
	}
}