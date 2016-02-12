#region References

using System;
using Scribe.Extensions;
using Scribe.Models.Entities;

#endregion

namespace Scribe.Models.Views
{
	public class PageHistorySummaryView
	{
		#region Properties

		public int Id { get; set; }

		public string LastModified { get; set; }

		public string ModifiedBy { get; set; }

		public int Number { get; set; }

		#endregion
	}
}