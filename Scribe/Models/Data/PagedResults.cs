#region References

using System.Collections.Generic;

#endregion

namespace Scribe.Models.Data
{
	public class PagedResults<T>
	{
		#region Properties

		public string Filter { get; set; }
		public int Page { get; set; }
		public int PerPage { get; set; }
		public IEnumerable<T> Results { get; set; }
		public int TotalCount { get; set; }
		public int TotalPages { get; set; }

		#endregion
	}
}