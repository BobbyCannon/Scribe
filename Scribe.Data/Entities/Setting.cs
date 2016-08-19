#region References

using Speedy;
using Speedy.Sync;

#endregion

namespace Scribe.Data.Entities
{
	public class Setting : SyncEntity
	{
		#region Properties

		public string Name { get; set; }

		public string Type { get; set; }

		public virtual User User { get; set; }

		public int? UserId { get; set; }

		public string Value { get; set; }

		#endregion
	}
}