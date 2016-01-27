#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Scribe.Models.Entities
{
	/// <summary>
	/// Represents a page. Content store in the page version.
	/// </summary>
	public class Page
	{
		#region Constructors

		[SuppressMessage("ReSharper", "DoNotCallOverridableMethodsInConstructor")]
		public Page()
		{
			History = new Collection<PageHistory>();
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the user who created the page.
		/// </summary>
		public virtual User CreatedBy { get; set; }

		/// <summary>
		/// Gets or sets the user ID for who created the page.
		/// </summary>
		public int CreatedById { get; set; }

		/// <summary>
		/// Gets or sets the date the page was created on.
		/// </summary>
		public DateTime CreatedOn { get; set; }

		/// <summary>
		/// Gets or sets the user who current editing the page.
		/// </summary>
		public virtual User EditingBy { get; set; }

		/// <summary>
		/// Gets or sets the user ID for who editing the page.
		/// </summary>
		public int? EditingById { get; set; }

		/// <summary>
		/// Gets or sets the date the page was last editing on.
		/// </summary>
		public DateTime EditingOn { get; set; }

		/// <summary>
		/// The versions of the pages.
		/// </summary>
		public virtual ICollection<PageHistory> History { get; set; }

		/// <summary>
		/// Gets or sets the page's unique ID.
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets whether the page is locked for administrator only editing.
		/// </summary>
		public bool IsLocked { get; set; }

		/// <summary>
		/// Gets or sets the user who last modified the page.
		/// </summary>
		public virtual User ModifiedBy { get; set; }

		/// <summary>
		/// Gets or sets the user ID of who last modified the page.
		/// </summary>
		public int ModifiedById { get; set; }

		/// <summary>
		/// Gets or sets the date the page was last modified on.
		/// </summary>
		public DateTime ModifiedOn { get; set; }

		/// <summary>
		/// Gets or sets the tags for the page, in the format ",tag1,tag2,tag3," (no spaces between tags).
		/// </summary>
		public string Tags { get; set; }

		/// <summary>
		/// The current markdown text for the page.
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// Gets or sets the title.
		/// </summary>
		/// <value>
		/// The title.
		/// </value>
		public string Title { get; set; }

		#endregion
	}
}