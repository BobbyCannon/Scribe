#region References

using System;

#endregion

namespace Scribe.Models.Data
{
	/// <summary>
	/// Represents a notification message.
	/// </summary>
	public class Notification
	{
		#region Properties

		/// <summary>
		/// The content of the message.
		/// </summary>
		public string Content { get; set; }

		/// <summary>
		/// The target for this message.
		/// </summary>
		public string Target { get; set; }

		/// <summary>
		/// The title for this message.
		/// </summary>
		public string Title { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		/// A string that represents the current object.
		/// </returns>
		public override string ToString()
		{
			return string.Format("{1}{0}{2}{0}{3}", Environment.NewLine, Target, Title, Content);
		}

		#endregion
	}
}