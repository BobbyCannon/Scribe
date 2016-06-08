#region References

using Scribe.Data.Entities;
using Speedy;

#endregion

namespace Scribe.Data
{
	public interface IScribeDatabase : IDatabase
	{
		#region Properties

		IRepository<Event> Events { get; }
		IRepository<EventValue> EventValues { get; }
		IRepository<File> Files { get; }
		IRepository<Page> Pages { get; }
		IRepository<PageVersion> PageVersions { get; }
		IRepository<Setting> Settings { get; }
		IRepository<User> Users { get; }

		#endregion
	}
}