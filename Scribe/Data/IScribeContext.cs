#region References

using System;
using System.Data.Entity;
using Scribe.Models.Entities;

#endregion

namespace Scribe.Data
{
	public interface IScribeContext : IDisposable
	{
		#region Properties

		DbSet<File> Files { get; }
		DbSet<Page> Pages { get; }
		DbSet<PageHistory> PageVersions { get; }
		DbSet<Setting> Settings { get; }
		DbSet<User> Users { get; }

		#endregion

		#region Methods

		int SaveChanges();

		#endregion
	}
}