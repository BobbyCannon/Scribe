#region References

using Scribe.Models.Views;

#endregion

namespace Scribe.Services
{
	public interface ISearchService
	{
		#region Methods

		/// <summary>
		/// Adds the specified page to the search index.
		/// </summary>
		/// <param name="page"> The page to add. </param>
		void Add(PageView page);

		/// <summary>
		/// Deletes the specified page from the search indexes.
		/// </summary>
		/// <param name="page"> The page to remove. </param>
		int Delete(PageView page);

		/// <summary>
		/// Creates the initial search index based on all pages in the system.
		/// </summary>
		void Initialize();

		/// <summary>
		/// Searches the lucene index with the search text.
		/// </summary>
		/// <param name="searchText"> The text to search with. </param>
		SearchView Search(string searchText);

		#endregion
	}
}