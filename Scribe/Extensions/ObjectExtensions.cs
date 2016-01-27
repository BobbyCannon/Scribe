#region References

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

#endregion

namespace Scribe.Extensions
{
	public static class ObjectExtensions
	{
		#region Methods

		/// <summary>
		/// Loop through collection and run action on each item.
		/// </summary>
		/// <typeparam name="T"> The type of the item. </typeparam>
		/// <param name="collection"> The collection to enumerate. </param>
		/// <param name="action"> The action to run on each item. </param>
		public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach (var item in collection)
			{
				action(item);
			}
		}

		public static JsonSerializerSettings GetSerializerSettings(bool camelCase = true)
		{
			var response = new JsonSerializerSettings();
			response.Converters.Add(new IsoDateTimeConverter());
			response.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
			response.PreserveReferencesHandling = PreserveReferencesHandling.Objects;

			if (camelCase)
			{
				response.Converters.Add(new StringEnumConverter { CamelCaseText = true });
				response.ContractResolver = new CamelCasePropertyNamesContractResolver();
			}

			return response;
		}

		/// <summary>
		/// Converts the item to JSON.
		/// </summary>
		/// <typeparam name="T"> The type of the item to convert. </typeparam>
		/// <param name="item"> The item to convert. </param>
		/// <param name="camelCase"> The flag to determine if we should use camel case or not. </param>
		/// <param name="indented"> The flag to determine if the JSON should be indented or not. </param>
		/// <returns> The JSON value of the item. </returns>
		public static string ToJson<T>(this T item, bool camelCase = true, bool indented = false)
		{
			return JsonConvert.SerializeObject(item, indented ? Formatting.Indented : Formatting.None, GetSerializerSettings(camelCase));
		}

		#endregion
	}
}