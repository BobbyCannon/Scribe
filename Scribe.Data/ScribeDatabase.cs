#region References

using Scribe.Models.Entities;
using Speedy;

#endregion

namespace Scribe.Data
{
	public class ScribeDatabase : Database, IScribeDatabase
	{
		#region Constructors

		public ScribeDatabase(string filePath = null, DatabaseOptions options = null)
			: base(filePath, options)
		{
		}

		#endregion

		#region Properties

		public IRepository<File> Files => GetRepository<File>();
		public IRepository<Page> Pages => GetRepository<Page>();
		public IRepository<PageVersion> PageVersions => GetRepository<PageVersion>();
		public IRepository<Setting> Settings => GetRepository<Setting>();
		public IRepository<User> Users => GetRepository<User>();

		#endregion
	}
}