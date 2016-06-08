#region References

using System;

#endregion

namespace Scribe.Website.Models
{
	public class PostmanRequest
	{
		#region Properties

		public Guid CollectionId { get; set; }
		public string Data { get; set; }
		public string DataMode { get; set; }
		public string Description { get; set; }
		public string Headers { get; set; }
		public Guid Id { get; set; }
		public string Method { get; set; }
		public string Name { get; set; }
		public long Timestamp { get; set; }
		public string Url { get; set; }

		#endregion
	}
}