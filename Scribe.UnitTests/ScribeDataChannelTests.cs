#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Bloodhound.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Data;

#endregion

namespace Scribe.UnitTests
{
	[TestClass]
	public class ScribeDataChannelTests
	{
		#region Methods

		[TestMethod]
		public void WriteSingleEvent()
		{
			var provider = TestHelper.GetDatabaseProvider();
			var channel = new ScribeDataChannel(provider);
			var currentTime = DateTime.UtcNow;

			channel.WriteEvents(new[]
			{
				new Event
				{
					CompletedOn = currentTime,
					CreatedOn = currentTime,
					Name = "Event",
					SessionId = Guid.Empty,
					Type = EventType.Event,
					UniqueId = Guid.NewGuid()
				}
			});
		}

		[TestMethod]
		public void WriteExistingEvent()
		{
			var provider = TestHelper.GetDatabaseProvider();
			var channel = new ScribeDataChannel(provider);
			var currentTime = DateTime.UtcNow;
			var id = Guid.NewGuid();

			using (var database = provider.GetDatabase())
			{
				database.Events.Add(new Data.Entities.Event
				{
					CreatedOn = currentTime,
					CompletedOn = currentTime,
					Name = "Event",
					ElapsedTicks = 0,
					SessionId = Guid.NewGuid(),
					StartedOn = currentTime,
					Type = EventType.Event,
					UniqueId = id,
				});

				database.SaveChanges();
			}

			channel.WriteEvents(new[]
			{
				new Event
				{
					CompletedOn = currentTime,
					CreatedOn = currentTime,
					Name = "Event2",
					SessionId = Guid.Empty,
					Type = EventType.Event,
					UniqueId = id,
					Values = new List<EventValue>
					{
						new EventValue("Test", "Value")
					}
				}
			});

			using (var database = provider.GetDatabase())
			{
				Assert.AreEqual(1, database.Events.Count());

				var actual = database.Events.First();
				Assert.AreEqual("Event2", actual.Name);
				Assert.AreEqual(1, actual.Values.Count);

				var actualValue = actual.Values.First();
				Assert.AreEqual("Test", actualValue.Name);
				Assert.AreEqual("Value", actualValue.Value);
			}
		}

		#endregion
	}
}