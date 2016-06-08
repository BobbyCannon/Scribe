#region References

using System.Collections.Generic;
using Bloodhound.Data;
using Scribe.Data.Entities;
using Speedy;
using BloodhoundEvent = Bloodhound.Models.Event;

#endregion

namespace Scribe.Data
{
	public class ScribeDataChannel : IDataChannel
	{
		#region Fields

		private readonly ScribeDatabaseProvider _provider;

		#endregion

		#region Constructors

		public ScribeDataChannel(ScribeDatabaseProvider provider)
		{
			_provider = provider;
		}

		#endregion

		#region Methods

		public void WriteEvents(IEnumerable<BloodhoundEvent> events)
		{
			using (var database = _provider.GetDatabase())
			{
				// ReSharper disable once AccessToDisposedClosure
				events.ForEach(x => AddEvent(database, x, null));
				database.SaveChanges();
			}
		}

		private static void AddEvent(IScribeDatabase database, BloodhoundEvent item, Event parent)
		{
			var entity = item.ToEntity(parent);
			database.Events.Add(entity);
			item.Children.ForEach(x => AddEvent(database, x, entity));
		}

		#endregion
	}
}