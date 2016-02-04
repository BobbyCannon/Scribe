#region References

using System.Collections.Generic;
using Scribe.Models.Views;

#endregion

namespace Scribe.Services
{
	public interface IScribeService
	{
		#region Methods

		void AddFile();
		PageView AddOrUpdatePage(PageView view);
		void DeleteFile(int id);
		void DeletePage(int id);
		void DeleteTag(string tag);
		IEnumerable<FileView> GetFiles(string filter);
		PageView GetPage(int id, bool includeHistory);
		IEnumerable<PageView> GetPages(string filter);
		IEnumerable<TagView> GetTags(string filter);
		void RenameTag(string oldTag, string newTag);

		#endregion
	}
}