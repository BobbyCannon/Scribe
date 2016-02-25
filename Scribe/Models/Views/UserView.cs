#region References

using System.Collections.Generic;

#endregion

namespace Scribe.Models.Views
{
	public class UserView
	{
		#region Properties

		public string DisplayName { get; set; }
		public string EmailAddress { get; set; }
		public int Id { get; set; }
		public IEnumerable<string> Tags { get; set; }
		public string UserName { get; set; }

		#endregion
	}
}