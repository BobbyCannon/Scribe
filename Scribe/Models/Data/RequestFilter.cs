#region References

using System;
using System.Collections.Generic;

#endregion

namespace Scribe.Models.Data
{
	public class RequestFilter : Dictionary<string, string>
	{
		#region Constructors

		private RequestFilter()
		{
		}

		#endregion

		#region Methods

		public static RequestFilter Parse(string value)
		{
			var response = new RequestFilter();
			var filters = value.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
			var id = Guid.NewGuid().ToString();

			foreach (var filter in filters)
			{
				var filterPieces = filter.Split('=');
				if (filterPieces.Length <= 1)
				{
					if (response.ContainsKey(id))
					{
						response[id] += filterPieces[0];
					}
					else
					{
						response.Add(id, filterPieces[0]);
					}
					continue;
				}

				response.Add(filterPieces[0].Trim(), filterPieces[1]);
			}

			return response;
		}

		#endregion
	}
}