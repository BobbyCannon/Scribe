using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scribe.Data.Entities;

namespace Scribe.Data
{
	public static class Extensions
	{
		/// <summary>
		/// Add a list of items to the collection.
		/// </summary>
		/// <typeparam name="T"> The type of the items in the collection. </typeparam>
		/// <param name="collection"> The collection to add to. </param>
		/// <param name="items"> The items to be added. </param>
		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
		{
			foreach (var item in items)
			{
				collection.Add(item);
			}
		}

		public static Event ToEntity(this Bloodhound.Models.Event item, Event parent = null)
		{
			var response = new Event
			{
				CompletedOn = item.CompletedOn,
				Name = item.Name,
				Parent = parent,
				StartedOn = item.CreatedOn,
				UniqueId = item.UniqueId,
				Type = item.Type,
				Values = item.Values
					.Where(x => !string.IsNullOrWhiteSpace(x.Value))
					.Select(x => new EventValue(x.Name,x.Value)).ToList()
			};

			response.Children.AddRange(item.Children.Select(x => ToEntity(x, response)));
			return response;
		}
	}
}
