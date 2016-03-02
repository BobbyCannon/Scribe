#region References

using System.Data.Entity;
using EasyDataFramework;
using EasyDataFramework.EntityFramework;
using Scribe.Data.Mapping;
using Scribe.Models.Entities;

#endregion

namespace Scribe.Data
{
	public class ScribeContext : EntityFrameworkDataContext, IScribeContext
	{
		#region Constructors

		public ScribeContext()
			: base("Name=DefaultConnection")
		{
		}

		#endregion

		#region Properties

		public IRepository<File> Files => GetRepository<File>();
		public IRepository<Page> Pages => GetRepository<Page>();
		public IRepository<Setting> Settings => GetRepository<Setting>();
		public IRepository<User> Users => GetRepository<User>();

		#endregion

		#region Methods

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Configurations.Add(new FileMap());
			modelBuilder.Configurations.Add(new PageMap());
			modelBuilder.Configurations.Add(new SettingsMap());
			modelBuilder.Configurations.Add(new UserMap());
		}

		#endregion
	}
}