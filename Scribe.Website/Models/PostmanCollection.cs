#region References

using System;
using System.Collections.ObjectModel;

#endregion

namespace Scribe.Website.Models
{
	public class PostmanCollection
	{
		#region Properties

		public Guid Id { get; set; }
		public string Name { get; set; }
		public Collection<PostmanRequest> Requests { get; set; }
		public long Timestamp { get; set; }

		#endregion
	}
}