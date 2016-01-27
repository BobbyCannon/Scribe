#region References

using System;
using Scribe.Extensions;
using Scribe.Models.Entities;

#endregion

namespace Scribe.Models.Views
{
	public class PageHistorySummaryView
	{
		#region Constructors

		public PageHistorySummaryView(int number, PageHistory history)
		{
			Id = history.Id;
			LastModified = DateTime.UtcNow.Subtract(history.EditedOn).ToTimeAgo();
			ModifiedBy = history.EditedBy.DisplayName;
			Number = number;
		}

		#endregion

		#region Properties

		public int Id { get; set; }

		public string LastModified { get; set; }

		public string ModifiedBy { get; set; }

		public int Number { get; set; }

		#endregion
	}
}