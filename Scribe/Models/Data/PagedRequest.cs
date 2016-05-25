#region References

using System;
using System.Collections.Generic;

#endregion

namespace Scribe.Models.Data
{
	public class PagedRequest
	{
		#region Constructors

		public PagedRequest()
		{
			Expand = new string[0];
			Filter = string.Empty;
			FilterValues = new object[0];
			Page = 1;
			PerPage = 20;
			Order = string.Empty;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Request filter values.
		/// Example: ["blah", 1]
		/// </summary>
		public IEnumerable<string> Expand { get; set; }

		/// <summary>
		/// Request filters should be in the follow format.
		/// Item1 = @0 && Item2 = @1
		/// Example: Tags == @0 && Status == @1
		/// Example: Tags == "blah" && Status == 1
		/// </summary>
		public string Filter { get; set; }

		/// <summary>
		/// Request filter values.
		/// Example: ["blah", 1]
		/// </summary>
		public IEnumerable<object> FilterValues { get; set; }

		/// <summary>
		/// Request order should be in the follow format.
		/// Item1;Item2=Ascending;Item3=Descending;
		/// Example: CreatedOn;Title=Descending;
		/// </summary>
		public string Order { get; set; }

		/// <summary>
		/// The page to load.
		/// </summary>
		public int Page { get; set; }

		/// <summary>
		/// The items to get per page.
		/// </summary>
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