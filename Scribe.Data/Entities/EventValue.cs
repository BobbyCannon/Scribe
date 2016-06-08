#region References

using System;
using Speedy;

#endregion

namespace Scribe.Data.Entities
{
	/// <summary>
	/// Represents a value for an event.
	/// </summary>
	[Serializable]
	public class EventValue : Entity
	{
		#region Constructors

		/// <summary>
		/// Instantiates a new instance of the class.
		/// </summary>
		public EventValue()
		{
		}

		/// <summary>
		/// Instantiates a new instance of the class.
		/// </summary>
		public EventValue(string name, string value)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name), "The name cannot be null.");
			}

			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentException("The name is required.", nameof(name));
			}

			if (value == null)
			{
				throw new ArgumentNullException(nameof(value), "The value cannot be null.");
			}

			Name = name;
			Value = value;
		}

		/// <summary>
		/// Instantiates a new instance of the class.
		/// </summary>
		public EventValue(string name, object value)
			: this(name, value?.ToString() ?? string.Empty)
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the event.
		/// </summary>
		public virtual Event Event { get; set; }

		/// <summary>
		/// Gets or sets the event ID.
		/// </summary>
		public int EventId { get; set; }

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		public string Value { get; set; }

		#endregion
	}
}