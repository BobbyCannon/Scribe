#region References

using System.Collections.Generic;
using System.Linq;

#endregion

namespace Scribe.Models.Data
{
	public class PagedResults<T>
	{
		#region Properties

		public string Filter { get; set; }
		public bool HasMore => Page != TotalPages;
		public string Order { get; set; }
		public int Page { get; set; }
		public int PerPage { get; set; }
		public IEnumerable<T> Results { get; set; }
		public int TotalCount { get; set; }
		public int TotalPages => TotalCount > 0 ? TotalCount / PerPage + (TotalCount % PerPage > 0 ? 1 : 0) : 1;

		#endregion
	}
}