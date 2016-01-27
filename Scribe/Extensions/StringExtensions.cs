#region References

using System;

#endregion

namespace Scribe.Extensions
{
	public static class StringExtensions
	{
		#region Methods

		/// <summary>
		/// Converts the string to an int. If it cannot be parse it will return the default value.
		/// </summary>
		/// <param name="input"> The string to convert. </param>
		/// <param name="defaultValue"> The default value to return. Defaults to Guid.Empty. </param>
		/// <returns> The int value or the default value. </returns>
		public static Guid ConvertToGuid(this string input, Guid? defaultValue = null)
		{
			Guid response;
			return !Guid.TryParse(input, out response) ? defaultValue ?? Guid.Empty : response;
		}

		/// <summary>
		/// Converts the string to an int. If it cannot be parse it will return the default value.
		/// </summary>
		/// <param name="input"> The string to convert. </param>
		/// <param name="defaultValue"> The default value to return. Defaults to 0. </param>
		/// <returns> The int value or the default value. </returns>
		public static int ConvertToInt(this string input, int defaultValue = 0)
		{
			int response;
			return !int.TryParse(input, out response) ? defaultValue : response;
		}

		#endregion
	}
}