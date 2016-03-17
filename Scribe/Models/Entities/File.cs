#region References

using System;
using EasyDataFramework;
using Scribe.Models.Views;

#endregion

namespace Scribe.Models.Entities
{
	public class File : Entity
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
		/// Gets the binary data for the file.
		/// </summary>
		public byte[] Data { get; set; }

		/// <summary>
		/// Flag to indicated this pages has been "soft" deleted.
		/// </summary>
		public bool IsDeleted { get; set; }

		/// <summary>
		/// Gets or sets the user who last modified the file.
		/// </summary>
		public virtual User ModifiedBy { get; set; }

		/// <summary>
		/// Gets or sets the user ID of who last modified the file.
		/// </summary>
		public int ModifiedById { get; set; }

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

		#region Methods

		public FileView ToView(bool includeData = false)
		{
			var response = new FileView
			{
				Id = Id,
				ModifiedOn = ModifiedOn,
				Name = Name,
				NameForLink = PageView.ConvertTitleForLink(Name),
				Size = Size / 1024 + " kb",
				Type = Type
			};

			if (includeData)
			{
				response.Data = new byte[Data.Length];
				Array.Copy(Data, response.Data, Data.Length);
			}

			return response;
		}

		#endregion
	}
}