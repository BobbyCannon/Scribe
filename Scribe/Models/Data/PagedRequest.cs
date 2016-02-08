namespace Scribe.Models.Data
{
	public class PagedRequest
	{
		#region Constructors

		public PagedRequest(string filter = "", int page = 1, int perPage = 20, bool includeDetails = false)
		{
			Filter = filter;
			Page = page;
			PerPage = perPage;
			IncludeDetails = includeDetails;
		}

		#endregion

		#region Properties

		public string Filter { get; set; }
		public bool IncludeDetails { get; set; }
		public int Page { get; set; }
		public int PerPage { get; set; }

		#endregion
	}
}