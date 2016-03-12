#region References

using System;
using System.Collections.Generic;

#endregion

namespace Scribe.Models.Data
{
	public class RequestOptions : Dictionary<string, string>
	{
		#region Constructors

		private RequestOptions()
		{
		}

		#endregion

		#region Methods

		public static RequestOptions Parse(string value)
		{
			var response = new RequestOptions();
			var sections = value.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var filter in sections)
			{
				var filterPieces = filter.Split('=');
				if (filterPieces.Length <= 1)
				{
					if (response.ContainsKey(filterPieces[0]))
					{
						response[filterPieces[0]] += filterPieces[0];
					}
					else
					{
						response.Add(filterPieces[0], filterPieces[0]);
					}
					continue;
				}

				response.Add(filterPieces[0], filterPieces[1]);
			}

			return response;
		}

		#endregion
	}
}