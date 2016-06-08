#region References

using System;
using System.Collections.Generic;
using Scribe.Website.Models;

#endregion

namespace Scribe.Website.ViewModels
{
	public class AnalyticsHomeView
	{
		#region Properties

		public DateTime EndDate { get; set; }

		public IEnumerable<AnalyticData<int>> MostActiveEditors { get; set; }

		public IEnumerable<AnalyticData<int>> MostActiveGroups { get; set; }

		public IEnumerable<AnalyticData<int>> MostActivePages { get; set; }

		public IEnumerable<AnalyticData<int>> MostViewPages { get; set; }

		public IEnumerable<AnalyticData<int>> NewPagesByUser { get; set; }

		public IEnumerable<AnalyticData<int>> NewPagesPerMonth { get; set; }

		public DateTime StartDate { get; set; }

		#endregion
	}
}