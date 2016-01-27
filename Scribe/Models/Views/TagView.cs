namespace Scribe.Models.Views
{
	public class TagView
	{
		#region Properties

		public string Class { get; set; }

		public string Tag { get; set; }

		#endregion

		#region Methods

		public static TagView Create(string text, int count)
		{
			var response = new TagView();
			response.Tag = text;

			if (count > 10)
			{
				response.Class = "tag5";
			}
			else if (count > 5)
			{
				response.Class = "tag4";
			}
			else if (count > 3)
			{
				response.Class = "tag3";
			}
			else if (count > 1)
			{
				response.Class = "tag2";
			}
			else
			{
				response.Class = "tag1";
			}

			return response;
		}

		#endregion
	}
}