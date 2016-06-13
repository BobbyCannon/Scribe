#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Bloodhound.Models;
using Speedy;

#endregion

namespace Scribe.Data.Entities
{
	/// <summary>
	/// Represents an event. There are three types (<seealso cref="EventType" />) of events of Session, Event, and Exception.
	/// </summary>
	[Serializable]
	public class Event : Entity
	{
		#region Constructors

		/// <summary>
		/// Instantiates a new instance of the class.
		/// </summary>
		[SuppressMessage("ReSharper", "DoNotCallOverridableMethodsInConstructor")]
		public Event()
		{
			Children = new Collection<Event>();
			Values = new Collection<EventValue>();
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or set the child events.
		/// </summary>
		public virtual ICollection<Event> Children { get; set; }

		/// <summary>
		/// Gets or set the date and time the event was completed.
		/// </summary>
		public DateTime CompletedOn { get; set; }

		/// <summary>
		/// Gets the elapsed ticks. The CompletedOn property will be updated.
		/// </summary>
		public long ElapsedTicks
		{
			get { return ElapsedTime.Ticks; }
			set
			{
				/* Only used for storage */
			}
		}

		/// <summary>
		/// Gets the elapsed time between the started and completed date and time.
		/// </summary>
		public TimeSpan ElapsedTime => CompletedOn - StartedOn;

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the parent.
		/// </summary>
		public virtual Event Parent { get; set; }

		/// <summary>
		/// Gets or sets the parent ID.
		/// </summary>
		public int? ParentId { get; set; }

		/// <summary>
		/// Gets or sets the session ID.
		/// </summary>
		public Guid SessionId { get; set; }

		/// <summary>
		/// Gets or set the date and time the event was started.
		/// </summary>
		public DateTime StartedOn { get; set; }

		/// <summary>
		/// Gets or sets the event type.
		/// </summary>
		public EventType Type { get; set; }

		/// <summary>
		/// Gets or sets the unique ID.
		/// </summary>
		public Guid UniqueId { get; set; }

		/// <summary>
		/// Gets or sets the values.
		/// </summary>
		public virtual ICollection<EventValue> Values { get; set; }

		#endregion

		#region Methods

		public void Update(Event entity)
		{
			CompletedOn = entity.CompletedOn;
			ElapsedTicks = entity.ElapsedTicks;
			Name = entity.Name;
			SessionId = entity.SessionId;
			StartedOn = entity.StartedOn;
			Type = entity.Type;

			var valuesToRemove = Values.Where(x => entity.Values.Any(y => y.Name == x.Name));
			valuesToRemove.ForEach(x => Values.Remove(x));

			foreach (var item in entity.Values)
			{
				var value = Values.FirstOrDefault(x => x.Name != item.Name);
				if (value == null)
				{
					Values.Add(item);
					continue;
				}

				value.Value = item.Value;
			}
		}

		#endregion
	}
}