#region References

using EasyDataFramework;
using Scribe.Models.Entities;

#endregion

namespace Scribe.Data
{
	public class ScribeMemoryContext : MemoryDataContext, IScribeContext
	{
		#region Properties

		public IRepository<File> Files => GetRepository<File>();
		public IRepository<Page> Pages => GetRepository<Page>();
		public IRepository<PageHistory> PageVersions => GetRepository<PageHistory>();
		public IRepository<Setting> Settings => GetRepository<Setting>();
		public IRepository<User> Users => GetRepository<User>();

		#endregion
	}
}