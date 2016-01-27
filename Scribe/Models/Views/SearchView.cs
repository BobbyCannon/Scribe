#region References

using System.Collections.Generic;

#endregion

namespace Scribe.Models.Views
{
	public class SearchView
	{
		#region Constructors

		public SearchView(string term)
		{
			Term = term;
			Results = new List<SearchResultView>();
		}

		#endregion

		#region Properties

		public ICollection<SearchResultView> Results { get; set; }
		public string Term { get; set; }

		#endregion
	}
}