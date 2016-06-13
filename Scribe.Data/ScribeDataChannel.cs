#region References

using System;
using System.Collections.Generic;
using System.Linq;
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

			try
			{
				using (var database = _provider.GetDatabase())
				{
					events.ForEach(x => AddEvent(database, x, null));
					database.SaveChanges();
				}
			}
			catch
			{
				WriteEventsIndividually(events);
			}
		}

		private static void AddEvent(IScribeDatabase database, BloodhoundEvent item, Event parent)
		{
			var entity = item.ToEntity(parent);
			database.Events.Add(entity);
			item.Children.ForEach(x => AddEvent(database, x, entity));
		}

		private static void AddOrUpdateEvent(IScribeDatabase database, BloodhoundEvent item, Event parent)
		{
			var entity = item.ToEntity(parent);
			var existingEvent = database.Events.FirstOrDefault(x => x.UniqueId == item.UniqueId);

			if (existingEvent != null)
			{
				existingEvent.Update(entity);
			}
			else
			{
				database.Events.Add(entity);
			}

			item.Children.ForEach(x => AddOrUpdateEvent(database, x, entity));
		}

		private void WriteEventsIndividually(IEnumerable<BloodhoundEvent> events)
		{
			using (var database = _provider.GetDatabase())
			{
				foreach (var x in events)
				{
					try
					{
						AddOrUpdateEvent(database, x, null);
						database.SaveChanges();
					}
					catch (Exception)
					{
						// log?
					}
				}
			}
		}
		#endregion
	}
}