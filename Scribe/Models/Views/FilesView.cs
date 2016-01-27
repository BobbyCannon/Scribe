#region References

using System.Collections.Generic;

#endregion

namespace Scribe.Models.Views
{
	public class FilesView
	{
		#region Properties

		public IEnumerable<FileView> Files { get; set; }

		#endregion
	}
}