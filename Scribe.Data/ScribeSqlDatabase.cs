#region References

using System.Data.Entity;
using Scribe.Data.Entities;
using Scribe.Data.Mapping;
using Speedy;
using Speedy.EntityFramework;

#endregion

namespace Scribe.Data
{
	public class ScribeSqlDatabase : EntityFrameworkDatabase, IScribeDatabase
	{
		#region Constructors

		public ScribeSqlDatabase()
			: this("Name=DefaultConnection", new DatabaseOptions())
		{
		}

		public ScribeSqlDatabase(string connectionString, DatabaseOptions options)
			: base(connectionString, options)
		{
		}

		#endregion

		#region Properties

		public IRepository<Event> Events => GetRepository<Event>();
		public IRepository<EventValue> EventValues => GetRepository<EventValue>();
		public IRepository<File> Files => GetRepository<File>();
		public IRepository<Page> Pages => GetRepository<Page>();
		public IRepository<PageVersion> PageVersions => GetRepository<PageVersion>();
		public IRepository<Setting> Settings => GetRepository<Setting>();
		public IRepository<User> Users => GetRepository<User>();

		#endregion

		#region Methods

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Configurations.Add(new EventMap());
			modelBuilder.Configurations.Add(new EventValueMap());
			modelBuilder.Configurations.Add(new FileMap());
			modelBuilder.Configurations.Add(new PageMap());
			modelBuilder.Configurations.Add(new PageVersionMap());
			modelBuilder.Configurations.Add(new SettingsMap());
			modelBuilder.Configurations.Add(new UserMap());
		}

		#endregion
	}
}