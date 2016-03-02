#region References

using EasyDataFramework;
using Scribe.Models.Entities;

#endregion

namespace Scribe.Data
{
	public interface IScribeContext : IDataContext
	{
		#region Properties

		IRepository<File> Files { get; }
		IRepository<Page> Pages { get; }
		IRepository<Setting> Settings { get; }
		IRepository<User> Users { get; }

		#endregion
	}
}