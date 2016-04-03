#region References

using Scribe.Models.Entities;
using Speedy;

#endregion

namespace Scribe.Data
{
	public interface IScribeDatabase : IDatabase
	{
		#region Properties

		IRepository<File> Files { get; }
		IRepository<Page> Pages { get; }
		IRepository<PageVersion> PageVersions { get; }
		IRepository<Setting> Settings { get; }
		IRepository<User> Users { get; }

		#endregion
	}
}