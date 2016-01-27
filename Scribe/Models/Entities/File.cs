#region References

using System;

#endregion

namespace Scribe.Models.Entities
{
	public class File
	{
		#region Properties

		/// <summary>
		/// Gets or sets the user who created the file.
		/// </summary>
		public virtual User CreatedBy { get; set; }

		/// <summary>
		/// Gets or sets the user ID for who created the file.
		/// </summary>
		public int CreatedById { get; set; }

		/// <summary>
		/// Gets or sets the date the file was created on.
		/// </summary>
		public DateTime CreatedOn { get; set; }

		/// <summary>
		/// Gets the binary data for the file.
		/// </summary>
		public byte[] Data { get; set; }

		/// <summary>
		/// Get the ID of the file.
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the user who last modified the file.
		/// </summary>
		public virtual User ModifiedBy { get; set; }

		/// <summary>
		/// Gets or sets the user ID of who last modified the file.
		/// </summary>
		public int ModifiedById { get; set; }

		/// <summary>
		/// Gets or sets the date the page was last modified on.
		/// </summary>
		public DateTime ModifiedOn { get; set; }

		/// <summary>
		/// Gets the name of the file.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Get the size of the file. Number of bits.
		/// </summary>
		public int Size { get; set; }

		/// <summary>
		/// Gets the content type of the file. Example is "image/png".
		/// </summary>
		public string Type { get; set; }

		#endregion
	}
}