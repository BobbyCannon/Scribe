#region References

using Scribe.Models.Data;
using Scribe.Models.Views;

#endregion

namespace Scribe.Services
{
	public interface IScribeService
	{
		#region Methods

		PageView BeginEditingPage(int id);
		void CancelEditingPage(int id);
		void DeleteFile(int id);
		void DeletePage(int id);
		void DeleteTag(string name);
		FileView GetFile(int id, bool includeData = false);
		FileView GetFile(string name, bool includeData = false);
		PagedResults<FileView> GetFiles(PagedRequest request = null);
		PageView GetPage(int id, bool includeHistory = false);
		string GetPagePreview(PageView view);
		PagedResults<PageView> GetPages(PagedRequest request = null);
		PagedResults<TagView> GetTags(PagedRequest request = null);
		UserView GetUser(int id);
		PagedResults<UserView> GetUsers(PagedRequest request = null);
		void LogIn(Credentials login);
		void LogOut();
		void RenameTag(RenameValues values);
		int SaveFile(FileView view);
		PageView SavePage(PageView view);
		UserView SaveUser(UserView view);
		PageView UpdatePage(PageUpdate update);

		#endregion
	}
}