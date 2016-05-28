#region References

using System.Collections.Generic;

#endregion

namespace Scribe.Models.Data
{
	/// <summary>
	/// Represents a page of results for a paged request to a service.
	/// </summary>
	/// <typeparam name="T"> The type of the items in the results collection. </typeparam>
	public class PagedResults<T>
	{
		#region Properties

		/// <summary>
		/// The filter to limit the request to. Defaults to an empty filter.
		/// </summary>
		public string Filter { get; set; }

		/// <summary>
		/// The values for the filter. Defaults to an empty collection.
		/// </summary>
		public IEnumerable<object> FilterValues { get; set; }

		/// <summary>
		/// The value to determine if the request has more pages.
		/// </summary>
		public bool HasMore => Page != TotalPages;

		/// <summary>
		/// The order the results are in.
		/// </summary>
		public string Order { get; set; }

		/// <summary>
		/// The page of these results.
		/// </summary>
		public int Page { get; set; }

		/// <summary>
		/// The maximum items per page.
		/// </summary>
		public int PerPage { get; set; }

		/// <summary>
		/// The results for a paged request.
		/// </summary>
		public IEnumerable<T> Results { get; set; }

		/// <summary>
		/// The total count of items for the request.
		/// </summary>
		public int TotalCount { get; set; }

		/// <summary>
		/// The total count of pages for the request.
		/// </summary>
		public int TotalPages => TotalCount > 0 ? TotalCount / PerPage + (TotalCount % PerPage > 0 ? 1 : 0) : 1;

		#endregion
	}
}