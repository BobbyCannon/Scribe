#region References

using System;

#endregion

namespace Scribe.Models.Entities
{
	/// <summary>
	/// Represents a version of a page.
	/// </summary>
	public class PageHistory
	{
		#region Properties

		/// <summary>
		/// Gets or sets the user who edited this version of the page.
		/// </summary>
		public virtual User EditedBy { get; set; }

		/// <summary>
		/// Gets or sets the ID of the user who edited this version of the page.
		/// </summary>
		public int EditedById { get; set; }

		/// <summary>
		/// Gets or sets the date the version was edited on.
		/// </summary>
		public DateTime EditedOn { get; set; }

		/// <summary>
		/// Gets or sets the ID for the page version.
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the page the content belongs to.
		/// </summary>
		public virtual Page Page { get; set; }

		/// <summary>
		/// The ID of the page this version is for.
		/// </summary>
		public int PageId { get; set; }

		/// <summary>
		/// Gets or sets the markdown text for the page.
		/// </summary>
		public string Text { get; set; }

		#endregion
	}
}