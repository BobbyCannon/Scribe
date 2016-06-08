namespace Scribe.Website.Models
{
	public class AnalyticData<T>
	{
		#region Properties

		public string Link { get; set; }
		public string Name { get; set; }
		public T Value { get; set; }

		#endregion
	}
}