#region References

using Scribe.Models.Entities;

#endregion

namespace Scribe.Models.Views
{
	public class FileView
	{
		#region Properties

		public byte[] Data { get; set; }
		public int Id { get; set; }
		public string Name { get; set; }
		public string Size { get; set; }
		public string Type { get; set; }

		#endregion

		#region Methods

		public static FileView Create(File file, bool includeData = false)
		{
			var response = new FileView
			{
				Id = file.Id,
				Name = file.Name,
				Size = file.Size / 1024 + " kb",
				Type = file.Type
			};

			if (includeData)
			{
				response.Data = file.Data;
			}

			return response;
		}

		#endregion
	}
}