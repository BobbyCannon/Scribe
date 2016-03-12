#region References

using System;

#endregion

namespace Scribe.Models.Data
{
	public class PagedRequest
	{
		#region Constructors

		public PagedRequest(string filter = "", int page = 1, int perPage = 20, bool includeDetails = false, string order = "")
		{
			Filter = filter;
			Page = page;
			PerPage = perPage;
			IncludeDetails = includeDetails;
			Order = order;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Request filters should be in the follow format.
		/// Item1=Value1;Item2=Value2;
		/// Example: Tags=homepage;Status=approved;
		/// </summary>
		public string Filter { get; set; }

		public bool IncludeDetails { get; set; }

		/// <summary>
		/// Request order should be in the follow format.
		/// Item1;Item2=Ascending;Item3=Descending;
		/// Example: CreatedOn;Title=Descending;
		/// </summary>
		public string Order { get; set; }

		public int Page { get; set; }

		public int PerPage { get; set; }

		#endregion

		#region Methods

		public void Cleanup()
		{
			Cleanup(Filter, x => x == null, () => Filter = string.Empty);
			Cleanup(Order, x => x == null, () => Order = string.Empty);
			Cleanup(Page, x => x <= 0, () => Page = 1);
			Cleanup(PerPage, x => x <= 0, () => PerPage = 20);
		}

		private static void Cleanup<T>(T item, Func<T, bool> test, Action set)
		{
			if (test(item))
			{
				set();
			}
		}

		#endregion
	}
}