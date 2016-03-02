#region References

using System.Collections.Generic;
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
		public IRepository<Setting> Settings => GetRepository<Setting>();
		public IRepository<User> Users => GetRepository<User>();

		#endregion

		public ICollection<Page> PageVersionsFilter(Page entity, IEnumerable<Page> collection)
		{
			collection.ForEach(Pages.AddOrUpdate);
			return new RelationshipMemoryRepository<Page>(Pages, x => x.ParentId == entity.Id, x => x.ParentId = entity.Id);
		}
	}
}