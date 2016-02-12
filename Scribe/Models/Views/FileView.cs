#region References



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
	}
}