#region References

using System.Collections.Generic;
using System.Linq;
using Scribe.Models.Entities;

#endregion

namespace Scribe.Models.Views
{
	public class PageHistoryView
	{
		#region Constructors

		public PageHistoryView(Page page)
		{
			var index = page.History.Count;
			Id = page.Id;
			Title = page.Title;
			TitleForLink = PageView.ConvertTitleForLink(page.Title);
			Versions = page.History
				.OrderByDescending(x => x.Id)
				.Select(x => new PageHistorySummaryView(index--, x))
				.ToList();
		}

		#endregion

		#region Properties

		public int Id { get; set; }

		public string Title { get; set; }

		public string TitleForLink { get; set; }

		public IEnumerable<PageHistorySummaryView> Versions { get; set; }

		#endregion
	}
}