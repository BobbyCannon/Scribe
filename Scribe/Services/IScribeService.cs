#region References

using System.Collections.Generic;
using Scribe.Models.Data;
using Scribe.Models.Views;

#endregion

namespace Scribe.Services
{
	public interface IScribeService
	{
		#region Methods

		void CancelPage(int id);
		void DeleteFile(int id);
		void DeletePage(int id);
		void DeleteTag(string name);
		FileView GetFile(int id, bool includeData = false);
		FileView GetFile(string name, bool includeData = false);
		IEnumerable<FileView> GetFiles(string filter = null, bool includeData = false);
		PageView GetPage(int id, bool includeHistory = false);
		IEnumerable<PageView> GetPages(string filter = null);
		IEnumerable<TagView> GetTags(string filter = null);
		void LogIn(Credentials login);
		void LogOut();
		string Preview(PageView model);
		void RenameTag(RenameValues values);
		void SaveFile(FileData data);
		PageView SavePage(PageView view);

		#endregion
	}
}