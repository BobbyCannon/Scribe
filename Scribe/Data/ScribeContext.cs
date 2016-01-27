#region References

using System.Data.Entity;
using Scribe.Models.Entities;
using Scribe.Models.Entities.Mapping;

#endregion

namespace Scribe.Data
{
	public class ScribeContext : DbContext, IScribeContext
	{
		#region Constructors

		public ScribeContext()
			: base("Name=DefaultConnection")
		{
		}

		#endregion

		#region Properties

		public DbSet<File> Files { get; set; }
		public DbSet<Page> Pages { get; set; }
		public DbSet<PageHistory> PageVersions { get; set; }
		public DbSet<Setting> Settings { get; set; }
		public DbSet<User> Users { get; set; }

		#endregion

		#region Methods

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Configurations.Add(new FileMap());
			modelBuilder.Configurations.Add(new PageHistoryMap());
			modelBuilder.Configurations.Add(new PageMap());
			modelBuilder.Configurations.Add(new SettingsMap());
			modelBuilder.Configurations.Add(new UserMap());
		}

		#endregion
	}
}